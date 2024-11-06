import cv2
import mediapipe as mp
import numpy as np
import matplotlib.pyplot as plt
import socket

mp_drawing = mp.solutions.drawing_utils
mp_holistic = mp.solutions.holistic

def detect_okay_gesture():
    cap = cv2.VideoCapture(2)  # Open default camera

    with mp_holistic.Holistic(min_detection_confidence=0.5, min_tracking_confidence=0.5) as holistic:
        okay_points = []

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            # Convert the frame to RGB as required by MediaPipe
            image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            image.flags.writeable = False

            # Process the image to detect gestures
            results = holistic.process(image)

            # Convert back to BGR for rendering
            image.flags.writeable = True
            image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

            # Draw hand landmarks
            mp_drawing.draw_landmarks(image, results.right_hand_landmarks, mp_holistic.HAND_CONNECTIONS)
            mp_drawing.draw_landmarks(image, results.left_hand_landmarks, mp_holistic.HAND_CONNECTIONS)

            # Check if right hand landmarks are detected
            if results.right_hand_landmarks:
                hand_landmarks = results.right_hand_landmarks.landmark

                # Key landmarks for detecting the "okay" gesture
                thumb_tip = hand_landmarks[4]    # Thumb tip
                index_tip = hand_landmarks[8]    # Index finger tip
                middle_tip = hand_landmarks[12]  # Middle finger tip
                ring_tip = hand_landmarks[16]    # Ring finger tip
                pinky_tip = hand_landmarks[20]   # Pinky finger tip

                # Calculate Euclidean distance between thumb tip and index tip
                distance_thumb_index = np.sqrt((thumb_tip.x - index_tip.x) ** 2 + (thumb_tip.y - index_tip.y) ** 2)

                # Check that other fingers are extended or away from the circle (okay gesture shape)
                distance_middle_tip = np.sqrt((middle_tip.x - thumb_tip.x) ** 2 + (middle_tip.y - thumb_tip.y) ** 2)
                distance_ring_tip = np.sqrt((ring_tip.x - thumb_tip.x) ** 2 + (ring_tip.y - thumb_tip.y) ** 2)
                distance_pinky_tip = np.sqrt((pinky_tip.x - thumb_tip.x) ** 2 + (pinky_tip.y - thumb_tip.y) ** 2)

                # Thresholds for "okay" gesture
                # - Thumb and index tips close to each other
                # - Other fingers should be away
                if (distance_thumb_index < 0.05 and
                    distance_middle_tip > 0.1 and
                    distance_ring_tip > 0.1 and
                    distance_pinky_tip > 0.1):
                    
                    okay_points.append((thumb_tip.x, thumb_tip.y))
                    cv2.putText(image, 'Okay Gesture Detected', (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                        try:
                            s.connect(('localhost', 12345))
                            s.sendall(f"GESTURE:ok".encode('utf-8'))
                        except Exception as e:
                            print("Error sending gesture data:", e,f"GESTURE:ok".encode('utf-8'))

            # Display the image with annotations
            cv2.imshow('Gesture Detection', image)
            if cv2.waitKey(10) & 0xFF == ord('q'):
                break

    cap.release()
    cv2.destroyAllWindows()

    # Plot the "okay" gesture points if detected
    if okay_points:
        xs, ys = zip(*okay_points)
        plt.plot(xs, ys, 'o')
        plt.plot(xs, ys, '-')
        plt.gca().invert_yaxis()
        plt.title("Detected 'Okay' Gesture Points")
        plt.show()

# Run the function to start the camera and detect gestures
detect_okay_gesture()