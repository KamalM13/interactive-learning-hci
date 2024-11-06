import cv2
import mediapipe as mp
import pickle
import socket
from dollarpy import Recognizer, Point

# Load gesture templates
with open("F:/Uni/4th year/Hci/project/interactive-learning-hci/python-sockets/gesture_templates.pkl", "rb") as f:
    templates = pickle.load(f)

recognizer = Recognizer(templates)
mp_holistic = mp.solutions.holistic
mp_drawing = mp.solutions.drawing_utils

# Function to convert landmarks to points for DollarPy
def landmarks_to_points(landmarks):
    return [Point(lm.x, lm.y) for lm in landmarks.landmark]

# Initialize video capture and holistic model
cap = cv2.VideoCapture(1)

with mp_holistic.Holistic(min_detection_confidence=0.5, min_tracking_confidence=0.5) as holistic:
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            break

        frame_rgb = cv2.cvtColor(cv2.flip(frame, 1), cv2.COLOR_BGR2RGB)
        results = holistic.process(frame_rgb)
        annotated_image = frame.copy()

        best_gesture = None
        best_score = 0

        if results.left_hand_landmarks or results.right_hand_landmarks:
            landmarks = results.left_hand_landmarks or results.right_hand_landmarks
            points = landmarks_to_points(landmarks)
            gesture_result = recognizer.recognize(points)
            if gesture_result:
                gesture_name, score = gesture_result
                if score > best_score:
                    best_gesture, best_score = gesture_name, score

        if best_gesture:
            # Send gesture to app.py through a socket connection
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                try:
                    s.connect(('localhost', 12345))
                    s.sendall(f"GESTURE:{best_gesture}".encode('utf-8'))
                except Exception as e:
                    print("Error sending gesture data:", e,f"GESTURE:{best_gesture}".encode('utf-8'))

        cv2.imshow('Guestures cam', annotated_image)
        if cv2.waitKey(1) == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
