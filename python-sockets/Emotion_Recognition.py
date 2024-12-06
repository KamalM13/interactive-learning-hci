import cv2
from deepface import DeepFace

def emotionDetection():
    video_capture = cv2.VideoCapture(0)
    
    while True:
        
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to capture video frame.")
            break
                
        result = DeepFace.analyze(frame, actions = ["emotion"], enforce_detection=False)
        emotion = result[0]["dominant_emotion"]

        return emotion
        
