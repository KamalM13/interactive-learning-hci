import socket
import time
import struct
import threading
import queue
import bluetooth
import subprocess
import os
from ultralytics import YOLO
from deepface import DeepFace
import mediapipe as mp
from collections import Counter
from collections import deque
import face_recognition
import cv2
from queue import Empty, Queue
import Face_Recognition
import Emotion_Recognition
from live_ges import Recognizer, Point, next, previous

class SharedCamera:
    def __init__(self, camera_index=0):
        self.camera_index = camera_index
        self.capture = cv2.VideoCapture(camera_index)
        self.frame = None
        self.lock = threading.Lock()
        self.running = True

        if not self.capture.isOpened():
            print("Error: Could not open the camera.")
            self.running = False

        threading.Thread(target=self._update_frames).start()

        threading.Thread(target=self._show_camera).start()

    def _update_frames(self):
        while self.running:
            ret, frame = self.capture.read()
            if ret:
                with self.lock:
                    self.frame = frame
            else:
                print("Error: Could not read frame from camera.")

    def get_frame(self):
        with self.lock:
            return self.frame.copy() if self.frame is not None else None

    def _show_camera(self):
        while self.running:
            frame = self.get_frame()
            if frame is not None:
                cv2.imshow("Shared Camera Feed", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):  # Press 'q' to exit the feed
                self.running = False
                break
        self.release()

    def release(self):
        self.running = False
        self.capture.release()
        cv2.destroyAllWindows()

# BluetoothScanner: Handles Bluetooth scanning in a separate thread
class BluetoothScanner:
    def __init__(self, data_queue):
        self.data_queue = data_queue

    def start(self):
        threading.Thread(target=self.scan).start()

    def scan(self):
        print("Starting continuous Bluetooth scan...")
        while True:
            try:
                nearby_devices = bluetooth.discover_devices(lookup_names=True)
                self.data_queue.put(nearby_devices)
                time.sleep(1)
            except Exception as e:
                print(f"Bluetooth scan error: {e}")
                time.sleep(1)



# CommunicationHandler: Handles socket communication and data sending
class CommunicationHandler:
    def __init__(self, server_address=("localhost", 12345)):
        self.server_address = server_address

    def send_message(self, connection, message):
        try:
            message_bytes = message.encode("utf-8")
            message_length = struct.pack(">I", len(message_bytes))
            connection.sendall(message_length + message_bytes)
            connection.recv(1)  # Wait for acknowledgment
        except Exception as e:
            print(f"Error sending message: {e}")

    def send_data(self, connection, user_id, bluetooth_devices, detection_results, gesture_data='', emotion_data=''):
        self.send_message(connection, f"ID:{user_id}")
        for result in detection_results:
            self.send_message(connection, f"DE:{result['class']}")
        for addr, name in bluetooth_devices:
            self.send_message(connection, f"BT:{addr},{name}")
        if gesture_data:
            self.send_message(connection, f"GESTURE:{gesture_data}")
        if emotion_data:
            self.send_message(connection, f"EMOT:{emotion_data}")
        print("Data sent successfully.")


# YOLOHandler: Manages YOLO object detection
class YOLOHandler:
    def __init__(self, shared_camera, detection_queue, comm_handler):
        self.shared_camera = shared_camera
        self.detection_queue = detection_queue
        self.model = YOLO("yolo11n.pt")  # Replace with the correct YOLO model file
        self.previous_positions = {}  # Dictionary to store the previous position of detected objects
        self.comm_handler = comm_handler  # Communication handler for sending data
        self.server_address = ("localhost", 12345)  # Update with the actual address if needed
        self.connection = None

    def start(self):
        # Establish connection to the C# application
        threading.Thread(target=self._connect_to_server).start()
        # Start object detection
        threading.Thread(target=self.run).start()

    def _connect_to_server(self):
        try:
            self.connection = socket.create_connection(self.server_address)
            print(f"Connected to server at {self.server_address}")
        except Exception as e:
            print(f"Error connecting to server: {e}")

    def run(self):
        print("Starting YOLO object detection...")
        while True:
            frame = self.shared_camera.get_frame()
            if frame is None:
                continue

            results = self.model.predict(frame, verbose=False)

            for result in results:
                if hasattr(result, "boxes"):
                    for box in result.boxes:
                        x1, y1, x2, y2 = map(int, box.xyxy[0])
                        confidence = box.conf[0]
                        class_id = int(box.cls[0])

                        if self.model.names[class_id] == "person" or confidence < 0.65:
                            continue
                        detection_data = {
                            "class": self.model.names[class_id],
                            "confidence": confidence,
                        }
                        self.detection_queue.put(detection_data)

                        # Object label and coordinates
                        object_label = self.model.names[class_id]
                        center_x = (x1 + x2) // 2
                        center_y = (y1 + y2) // 2

                        # Track and send coordinates
                        coordinates = f"{object_label},{center_x},{center_y}"
                        print(f"Sending coordinates to server: {coordinates}")
                        self._send_coordinates_to_server(coordinates)

                        # Draw the rectangle and coordinates on the frame
                        label = f"{object_label}: {confidence:.2f}"
                        cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                        cv2.putText(frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)
                        cv2.putText(frame, f"({center_x}, {center_y})", (center_x + 10, center_y - 10),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 2)

            # Display the frame with updated object positions
            cv2.imshow("Object Detection with Movement", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):  # Press 'q' to exit
                break

    def _send_coordinates_to_server(self, coordinates):
        if self.connection:
            try:
                self.comm_handler.send_message(self.connection, f"COORDS:{coordinates}")
            except Exception as e:
                print(f"Error sending coordinates: {e}")
        else:
            print("No active connection to server.")

    def process_results(self, results, frame):
        for result in results:
            if hasattr(result, "boxes"):
                for box in result.boxes:
                    x1, y1, x2, y2 = map(int, box.xyxy[0])
                    confidence = box.conf[0]
                    class_id = int(box.cls[0])

                    if self.model.names[class_id] == "person" or confidence < 0.65:
                        continue

                    detection_data = {"class": self.model.names[class_id]}
                    self.detection_queue.put(detection_data)

                    label = f"{self.model.names[class_id]}: {confidence:.2f}"
                    cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                    cv2.putText(frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)




class EmotionDetectionHandler:
    def __init__(self, shared_camera):
        self.shared_camera = shared_camera
        self.current_emotion = None
        self.emotion_counter = Counter()

    def start(self):
        threading.Thread(target=self.run).start()

    def run(self):
        print("Starting emotion detection...")
        while True:
            frame = self.shared_camera.get_frame()
            if frame is None:
                continue

            try:
                # Analyze emotions in the current frame
                result = DeepFace.analyze(frame, actions=["emotion"], enforce_detection=False)
                emotions = result[0]["emotion"]
                dominant_emotion = result[0]["dominant_emotion"]

                # Update the emotion counter with the dominant emotion from this frame
                self.emotion_counter[dominant_emotion] += 1

                # Determine the most dominant emotion across all frames so far
                self.current_emotion = self.emotion_counter.most_common(1)[0][0]

                #self.draw_emotion_details(frame, emotions, self.current_emotion)
            except Exception as e:
                print(f"Emotion detection failed: {e}")


    def draw_emotion_details(self, frame, emotions, dominant_emotion):
        height, width, _ = frame.shape
        text_color = (255, 255, 255)
        bar_color = (0, 255, 0)
        bar_width = 200
        x_offset = 10
        y_offset = 50
        bar_height = 20
        spacing = 10

        cv2.putText(
            frame,
            f"Dominant Emotion: {dominant_emotion}",
            (x_offset, y_offset - 20),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.8,
            text_color,
            2,
            cv2.LINE_AA
        )

        for emotion, confidence in emotions.items():
            confidence_bar_length = int((confidence / 100) * bar_width)
            label = f"{emotion}: {confidence:.1f}%"

            cv2.rectangle(
                frame,
                (x_offset, y_offset),
                (x_offset + bar_width, y_offset + bar_height),
                (50, 50, 50),
                -1
            )
            cv2.rectangle(
                frame,
                (x_offset, y_offset),
                (x_offset + confidence_bar_length, y_offset + bar_height),
                bar_color,
                -1
            )
            cv2.putText(
                frame,
                label,
                (x_offset + bar_width + 10, y_offset + 15),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.5,
                text_color,
                1,
                cv2.LINE_AA
            )
            y_offset += bar_height + spacing


class GestureDetectionHandler:
    def __init__(self, shared_camera, recognizer):
        self.shared_camera = shared_camera
        self.recognizer = recognizer
        self.all_points = deque() 
        self.frame_count = 0
        self.mp_pose = mp.solutions.pose
        self.mp_drawing = mp.solutions.drawing_utils
        self.pose = self.mp_pose.Pose()
        self.gestures = None

    def start(self):
        threading.Thread(target=self.run).start()

    def run(self):
        print("Starting gesture detection...")
        while True:
            frame = self.shared_camera.get_frame()
            if frame is None:
                continue

            self.frame_count += 1
            try:
                # Convert the frame to RGB format
                rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

                # Process the frame with MediaPipe Pose
                results = self.pose.process(rgb_frame)

                if results.pose_landmarks:
                    # Extract wrist coordinates
                    image_height, image_width, _ = frame.shape
                    right_wrist = results.pose_landmarks.landmark[self.mp_pose.PoseLandmark.RIGHT_WRIST]
                    left_wrist = results.pose_landmarks.landmark[self.mp_pose.PoseLandmark.LEFT_WRIST]

                    self.all_points.append(Point(
                        int(right_wrist.x * image_width),
                        int(right_wrist.y * image_height),
                        1
                    ))

                    self.all_points.append(Point(
                        int(left_wrist.x * image_width),
                        int(left_wrist.y * image_height),
                        1
                    ))

                    # Recognize gesture every 30 frames
                    if self.frame_count % 15 == 0:
                        self.frame_count = 0
                        result = self.recognizer.recognize(self.all_points)
                        gesture_name, score = result
                        print(f"Detected gesture: {gesture_name}, Score: {score:.2f}")
                        self.all_points.clear()

                        if score > 0.3:
                            self.gestures=gesture_name
                            
            except Exception as e:
                print(f"Error in gesture detection: {e}")

    def get_gestures(self):
        return self.gestures


# ReactiVisionHandler: Manages the reactiVision subprocess
class ReactiVisionHandler:
    def __init__(self, executable_path):
        self.executable_path = executable_path

    def start(self):
        threading.Thread(target=self.run).start()

    def run(self):
        print("Starting reacTIVision...")
        try:
            subprocess.Popen([self.executable_path])
        except Exception as e:
            print(f"Error starting reacTIVision: {e}")


# UserAuthentication: Handles user login using face recognition
class FaceRecognition:
    def __init__(self, shared_camera):
        self.shared_camera = shared_camera  

    def login(self, users_img_encodings):
        ids = [user_id for user_id, encoding in users_img_encodings]
        encodings = [encoding for user_id, encoding in users_img_encodings]

        start_time = time.time()  # Record the start time for timeout

        while True:
            elapsed_time = time.time() - start_time
            if elapsed_time > 30: 
                print("Login timed out.")
                break

            frame = self.shared_camera.get_frame()
            if frame is None:
                continue

            
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

            face_locations = face_recognition.face_locations(rgb_frame)

            if not face_locations:
                continue 

            try:
                face_encodings = face_recognition.face_encodings(rgb_frame, face_locations)
            except Exception as e:
                print(f"Error during face encoding: {e}")
                continue

            for face_encoding in face_encodings:
                matches = face_recognition.compare_faces(encodings, face_encoding)
                if True in matches:
                    matched_index = matches.index(True)
                    user_id = ids[matched_index]

                    return user_id
     
        print("Login process ended without a match.")
        return None
    
    @staticmethod
    def create_image_encodings(users_id_to_img_path):
        users_img_encodings = []
        for user in users_id_to_img_path:
            user_id, img_path = user

            try:
                image = face_recognition.load_image_file(img_path)
                encodings = face_recognition.face_encodings(image)

                if encodings:
                    users_img_encodings.append((user_id, encodings[0]))
                else:
                    print(f"No face found in image: {img_path}")
            except Exception as e:
                print(f"Error processing image {img_path}: {e}")

        return users_img_encodings


# Application: Main application class
class Application:
    def __init__(self):
        self.bluetooth_queue = Queue()
        self.detection_queue = Queue()
        self.shared_camera = SharedCamera(camera_index=1)  
        self.yolo_handler = YOLOHandler(self.shared_camera, self.detection_queue)
        self.comm_handler = CommunicationHandler()
        self.reactivision_handler = ReactiVisionHandler(
            executable_path="C:/Users/KamalM12/Vscode/Hci Project/reacTIVision-1.5.1-win64/reacTIVision.exe"
        )
        self.emotion_handler = EmotionDetectionHandler(self.shared_camera)

        self.face_recognition = FaceRecognition(self.shared_camera)
        self.logged_in_user_id = None
        self.users = [(1, "../bin/Debug/person.jpg")]
        self.users_img_encodings = FaceRecognition.create_image_encodings(self.users)

        recognizer = Recognizer([next, previous])  # Replace with your gesture logic
        self.gesture_handler = GestureDetectionHandler(self.shared_camera, recognizer)

    def start(self):
        print("Starting application...")
        self.logged_in_user_id = self.face_recognition.login(self.users_img_encodings)
        if not self.logged_in_user_id:
            print("No user recognized. Exiting...")
            return

        print(f"Logged in user ID: {self.logged_in_user_id}")

        self.start_threads()

    def start_threads(self):
        self.reactivision_handler.start()
        BluetoothScanner(self.bluetooth_queue).start()
        self.yolo_handler.start()
        self.emotion_handler.start()
        self.gesture_handler.start()
        threading.Thread(target=self.client_thread).start()

    def client_thread(self):
        while True:
            try:
                with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
                    client_socket.connect(self.comm_handler.server_address)

                    while True:
                        bluetooth_devices = self.get_bluetooth_devices()
                        detection_results = self.get_detection_results()
                        gesture_data = self.gesture_handler.get_gestures()
                        emotion_data = self.emotion_handler.current_emotion
                        # Send data to client
                        self.comm_handler.send_data(
                            client_socket,
                            self.logged_in_user_id,
                            bluetooth_devices,
                            detection_results,
                            gesture_data=gesture_data,
                            emotion_data=emotion_data
                        )

                        # if emotion_data:
                        #     self.comm_handler.send_message(client_socket, f"EMOT:{emotion_data}")

                        time.sleep(0.5)
            except Exception as e:
                print(f"Connection error: {e}")
                time.sleep(2)

    def get_bluetooth_devices(self):
        try:
            return self.bluetooth_queue.get_nowait()
        except Empty:
            return "none"

    def get_detection_results(self):
        results = []
        while not self.yolo_handler.detection_queue.empty():
            results.append(self.yolo_handler.detection_queue.get())
        return results
    
    def get_user_emotions():
        return Emotion_Recognition.emotionDetection()
    
    def stop(self):
        self.shared_camera.release()


# Entry Point
if __name__ == "__main__":
    app = Application()
    try:
        app.start()
    except KeyboardInterrupt:
        print("Exiting...")
        app.stop()
        os._exit(0)
