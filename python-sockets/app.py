import socket
import time
import struct
import threading
import queue
import bluetooth
import subprocess
import os
from ultralytics import YOLO
import cv2
from queue import Empty, Queue
import Face_Recognition


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

    def send_data(self, connection, user_id, bluetooth_devices, detection_results, gesture_data=None):
        self.send_message(connection, f"ID:{user_id}")
        for result in detection_results:
            self.send_message(connection, f"DE:{result['class']}")
        for addr, name in bluetooth_devices:
            self.send_message(connection, f"BT:{addr},{name}")
        if gesture_data:
            self.send_message(connection, f"GESTURE:{gesture_data}")
        print("Data sent successfully.")


# YOLOHandler: Manages YOLO object detection
class YOLOHandler:
    def __init__(self, model_path="yolo11n.pt"):
        self.model = YOLO(model_path)
        self.detection_queue = Queue()

    def start(self):
        threading.Thread(target=self.run).start()

    def run(self):
        print("Starting YOLO object detection...")
        cap = cv2.VideoCapture(0)
        if not cap.isOpened():
            print("Error: Camera not accessible.")
            return

        while True:
            ret, frame = cap.read()
            if not ret:
                print("Error: Could not read frame.")
                break

            results = self.model.predict(frame)
            self.process_results(results, frame)
            cv2.imshow("YOLO Object Detection", frame)

            if cv2.waitKey(1) & 0xFF == ord("q"):
                break

        cap.release()
        cv2.destroyAllWindows()
        print("YOLO object detection ended.")

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
class UserAuthentication:
    @staticmethod
    def login():
        users = [(1, "../bin/Debug/person.jpg")]
        users_encodings = Face_Recognition.createImageEncodings(users)
        return Face_Recognition.login(users_encodings)


# Application: Main application class
class Application:
    def __init__(self):
        self.bluetooth_queue = Queue()
        self.yolo_handler = YOLOHandler()
        self.comm_handler = CommunicationHandler()
        self.react_handler = ReactiVisionHandler(
            "C:/Users/KamalM12/Vscode/Hci Project/reacTIVision-1.5.1-win64/reacTIVision.exe"
        )
        self.logged_in_user_id = None

    def start(self):
        self.logged_in_user_id = UserAuthentication.login()
        if not self.logged_in_user_id:
            print("No user recognized. Exiting...")
            return

        self.start_threads()

    def start_threads(self):
        BluetoothScanner(self.bluetooth_queue).start()
        self.yolo_handler.start()
        #self.react_handler.start()  # Start the reacTIVision thread
        threading.Thread(target=self.client_thread, daemon=True).start()

    def client_thread(self):
        while True:
            try:
                with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
                    client_socket.connect(self.comm_handler.server_address)

                    while True:
                        bluetooth_devices = self.get_bluetooth_devices()
                        detection_results = self.get_detection_results()
                        self.comm_handler.send_data(
                            client_socket,
                            self.logged_in_user_id,
                            bluetooth_devices,
                            detection_results,
                        )
                        time.sleep(0.5)
            except Exception as e:
                print(f"Connection error: {e}")
                time.sleep(2)

    def get_bluetooth_devices(self):
        try:
            return self.bluetooth_queue.get_nowait()
        except Empty:
            return []

    def get_detection_results(self):
        results = []
        while not self.yolo_handler.detection_queue.empty():
            results.append(self.yolo_handler.detection_queue.get())
        return results


# Entry Point
if __name__ == "__main__":
    try:
        app = Application()
        app.start()
    except KeyboardInterrupt:
        print("Exiting...")
        os._exit(0)
