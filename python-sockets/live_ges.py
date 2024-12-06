"""import cv2
import mediapipe as mp
import pickle
import socket
from dollarpy import Recognizer, Point

# Load gesture templates
with open("f:/Uni/4th year/Hci/project/interactive-learning-hci/python-sockets/gesture_templates.pkl", "rb") as f:
    templates = pickle.load(f)

recognizer = Recognizer(templates)
mp_holistic = mp.solutions.holistic
mp_drawing = mp.solutions.drawing_utils

# Function to convert landmarks to points for DollarPy
def landmarks_to_points(landmarks):
    return [Point(lm.x, lm.y) for lm in landmarks.landmark]

# Initialize video capture and holistic model
cap = cv2.VideoCapture(0)

with mp_holistic.Holistic(min_detection_confidence=0.7, min_tracking_confidence=0.5) as holistic:
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            break

        frame_rgb = cv2.cvtColor(cv2.flip(frame, 1), cv2.COLOR_BGR2RGB)
        results = holistic.process(frame_rgb)
        annotated_image = frame.copy()

        #best_gesture = None
        #best_score = 0
        

        if results.left_hand_landmarks or results.right_hand_landmarks:
            landmarks = results.left_hand_landmarks or results.right_hand_landmarks
            points = landmarks_to_points(landmarks)
            gesture_result = recognizer.recognize(points)
            if gesture_result:
                gesture_name, score = gesture_result
                print(gesture_result)
                if score>0.6:
                    print("WWWWWWWWWWWWWWWWWWWWW ",gesture_result)

        if gesture_result:
            # Send gesture to app.py through a socket connection
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                try:
                    s.connect(('localhost', 12345))
                    s.sendall(f"GESTURE:{gesture_result}".encode('utf-8'))
                except Exception as e:
                    print("Error sending gesture data:", e,f"GESTURE:{gesture_result}".encode('utf-8'))

        cv2.imshow('Guestures cam', annotated_image)
        if cv2.waitKey(1) == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
"""
import os
import cv2
import mediapipe as mp
import socket
# initialize Pose estimator
mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

pose = mp_pose.Pose(
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5)

cap = cv2.VideoCapture(0)
framecnt=0


from dollarpy import Recognizer, Template, Point
next = Template('next', [
Point(116,229, 1),
Point(310,481, 1),
Point(116,229, 1),
Point(310,482, 1),
Point(116,228, 1),
Point(310,483, 1),
Point(116,228, 1),
Point(309,484, 1),
Point(116,228, 1),
Point(309,484, 1),
Point(116,228, 1),
Point(309,485, 1),
Point(116,227, 1),
Point(309,485, 1),
Point(116,227, 1),
Point(308,485, 1),
Point(116,226, 1),
Point(307,485, 1),
Point(116,226, 1),
Point(307,485, 1),
Point(116,226, 1),
Point(308,485, 1),
Point(116,226, 1),
Point(309,484, 1),
Point(116,226, 1),
Point(309,484, 1),
Point(116,227, 1),
Point(309,484, 1),
Point(116,227, 1),
Point(308,484, 1),
Point(116,227, 1),
Point(308,484, 1),
Point(117,227, 1),
Point(308,484, 1),
Point(118,227, 1),
Point(308,484, 1),
Point(118,227, 1),
Point(308,484, 1),
Point(119,226, 1),
Point(309,483, 1),
Point(119,226, 1),
Point(310,483, 1),
Point(120,226, 1),
Point(310,483, 1),
Point(120,226, 1),
Point(311,483, 1),
Point(120,225, 1),
Point(312,484, 1),
Point(120,225, 1),
Point(312,484, 1),
Point(121,225, 1),
Point(314,484, 1),
Point(121,225, 1),
Point(314,484, 1),
Point(121,226, 1),
Point(315,484, 1),
Point(121,225, 1),
Point(314,485, 1),
Point(126,224, 1),
Point(314,483, 1),
Point(128,224, 1),
Point(315,482, 1),
Point(129,224, 1),
Point(315,482, 1),
Point(130,223, 1),
Point(314,481, 1),
Point(131,219, 1),
Point(314,481, 1),
Point(132,217, 1),
Point(315,480, 1),
Point(135,228, 1),
Point(323,480, 1),
Point(135,228, 1),
Point(326,480, 1),
Point(134,230, 1),
Point(328,479, 1),
Point(136,229, 1),
Point(328,476, 1),
Point(95,407, 1),
Point(338,476, 1),
Point(117,318, 1),
Point(342,477, 1),
Point(93,410, 1),
Point(343,487, 1),
Point(107,343, 1),
Point(341,486, 1),
Point(116,466, 1),
Point(349,503, 1),
Point(114,480, 1),
Point(347,506, 1),
Point(110,481, 1),
Point(336,508, 1),
Point(95,484, 1),
Point(336,509, 1),
Point(97,484, 1),
Point(336,508, 1),
Point(98,483, 1),
Point(334,510, 1),
Point(248,255, 1),
Point(297,262, 1),
Point(255,244, 1),
Point(286,237, 1),
Point(254,242, 1),
Point(280,224, 1),
Point(262,240, 1),
Point(289,251, 1),
Point(262,243, 1),
Point(310,294, 1),
Point(249,250, 1),
Point(276,223, 1),
Point(252,248, 1),
Point(257,194, 1),
Point(253,244, 1),
Point(269,240, 1),
Point(257,241, 1),
Point(293,280, 1),
Point(264,240, 1),
Point(269,201, 1),
Point(268,237, 1),
Point(335,471, 1),
Point(261,241, 1),
Point(340,471, 1),
Point(257,244, 1),
Point(337,456, 1),
Point(255,246, 1),
Point(333,435, 1),
Point(254,245, 1),
Point(331,429, 1),
Point(256,245, 1),
Point(329,424, 1),
Point(255,246, 1),
Point(328,279, 1),
Point(246,248, 1),
Point(323,233, 1),
Point(236,264, 1),
Point(320,231, 1),
Point(237,264, 1),
Point(319,256, 1),
Point(234,263, 1),
Point(320,229, 1),
Point(220,312, 1),
Point(323,209, 1),
Point(224,288, 1),
Point(318,219, 1),
Point(209,336, 1),
Point(316,219, 1),
Point(202,346, 1),
Point(316,217, 1),
Point(195,360, 1),
Point(316,217, 1),
Point(181,398, 1),
Point(315,216, 1),
Point(225,274, 1),
Point(325,224, 1),
Point(232,258, 1),
Point(323,216, 1),
Point(237,249, 1),
Point(320,220, 1),
Point(238,250, 1),
Point(320,213, 1),
Point(237,252, 1),
Point(320,211, 1),
Point(236,257, 1),
Point(319,212, 1),
Point(230,278, 1),
Point(318,213, 1),
Point(228,278, 1),
Point(317,212, 1),
Point(203,340, 1),
Point(316,211, 1),
Point(197,347, 1),
Point(316,212, 1),
Point(184,379, 1),
Point(316,213, 1),
Point(187,355, 1),
Point(314,211, 1),
Point(208,296, 1),
Point(312,210, 1),
Point(208,298, 1),
Point(312,210, 1),
Point(206,326, 1),
Point(311,210, 1),
Point(214,298, 1),
Point(309,209, 1),
])
previous = Template('previous', [
Point(106,485, 1),
Point(345,209, 1),
Point(113,493, 1),
Point(350,209, 1),
Point(121,493, 1),
Point(351,208, 1),
Point(118,496, 1),
Point(352,208, 1),
Point(117,492, 1),
Point(351,207, 1),
Point(119,496, 1),
Point(352,206, 1),
Point(115,505, 1),
Point(352,206, 1),
Point(116,506, 1),
Point(353,206, 1),
Point(113,508, 1),
Point(353,206, 1),
Point(112,509, 1),
Point(353,206, 1),
Point(111,511, 1),
Point(354,207, 1),
Point(110,512, 1),
Point(353,207, 1),
Point(109,513, 1),
Point(353,207, 1),
Point(109,511, 1),
Point(353,207, 1),
Point(109,509, 1),
Point(353,207, 1),
Point(109,509, 1),
Point(353,207, 1),
Point(108,511, 1),
Point(353,207, 1),
Point(107,512, 1),
Point(353,208, 1),
Point(104,515, 1),
Point(353,208, 1),
Point(105,513, 1),
Point(353,208, 1),
Point(105,513, 1),
Point(353,208, 1),
Point(106,513, 1),
Point(352,207, 1),
Point(106,515, 1),
Point(352,206, 1),
Point(106,516, 1),
Point(352,206, 1),
Point(105,516, 1),
Point(351,205, 1),
Point(109,517, 1),
Point(350,206, 1),
Point(111,517, 1),
Point(349,207, 1),
Point(111,517, 1),
Point(349,207, 1),
Point(113,517, 1),
Point(348,207, 1),
Point(113,518, 1),
Point(348,207, 1),
Point(113,518, 1),
Point(347,207, 1),
Point(117,516, 1),
Point(333,210, 1),
Point(118,516, 1),
Point(329,210, 1),
Point(119,516, 1),
Point(327,212, 1),
Point(120,516, 1),
Point(321,204, 1),
Point(120,516, 1),
Point(299,152, 1),
Point(120,514, 1),
Point(288,146, 1),
Point(119,510, 1),
Point(291,144, 1),
Point(118,507, 1),
Point(290,144, 1),
Point(116,507, 1),
Point(289,149, 1),
Point(116,507, 1),
Point(292,138, 1),
Point(117,496, 1),
Point(282,206, 1),
Point(121,482, 1),
Point(276,212, 1),
Point(124,487, 1),
Point(279,213, 1),
Point(171,264, 1),
Point(246,243, 1),
Point(197,230, 1),
Point(226,243, 1),
Point(200,231, 1),
Point(229,241, 1),
Point(205,234, 1),
Point(226,244, 1),
Point(210,232, 1),
Point(228,247, 1),
Point(197,249, 1),
Point(236,251, 1),
Point(112,349, 1),
Point(323,455, 1),
Point(124,190, 1),
Point(313,411, 1),
Point(94,359, 1),
Point(336,416, 1),
Point(92,343, 1),
Point(337,431, 1),
Point(107,315, 1),
Point(326,490, 1),
Point(118,132, 1),
Point(327,495, 1),
Point(127,157, 1),
Point(324,497, 1),
Point(116,213, 1),
Point(330,498, 1),
Point(123,114, 1),
Point(343,489, 1),
Point(100,229, 1),
Point(343,490, 1),
Point(106,94, 1),
Point(340,478, 1),
Point(110,87, 1),
Point(342,470, 1),
Point(112,88, 1),
Point(339,470, 1),
Point(114,79, 1),
Point(339,468, 1),
Point(110,110, 1),
Point(338,467, 1),
Point(111,118, 1),
Point(338,465, 1),
Point(111,118, 1),
Point(337,468, 1),
Point(109,122, 1),
Point(337,468, 1),
Point(110,132, 1),
Point(337,471, 1),
Point(110,133, 1),
Point(334,470, 1),
Point(110,133, 1),
Point(333,471, 1),
Point(110,134, 1),
Point(332,471, 1),
Point(110,134, 1),
Point(331,471, 1),
Point(110,133, 1),
Point(332,471, 1),
Point(111,132, 1),
Point(331,471, 1),
Point(111,131, 1),
Point(332,470, 1),
Point(111,125, 1),
Point(331,470, 1),
Point(111,123, 1),
Point(332,470, 1),
Point(111,124, 1),
Point(333,470, 1),
Point(111,127, 1),
Point(334,470, 1),
Point(112,128, 1),
Point(334,470, 1),
Point(112,130, 1),
Point(334,468, 1),
])
recognizer = Recognizer([next,previous])



Allpoints=[]








while cap.isOpened():
    # read frame from capture object
    ret, frame = cap.read()
    if not ret:
        print("Can't receive frame (stream end?). Exiting ...")
        break
    frame = cv2.resize(frame, (480, 320))
    framecnt+=1
    try:
        # convert the frame to RGB format
        RGB = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        #print (framecnt)
        # process the RGB frame to get the result
        results = pose.process(RGB)
            # Loop through the detected poses to visualize.
        #for idx, landmark in enumerate(results.pose_landmarks.landmark):
            #print(f"{mp_pose.PoseLandmark(idx).name}: (x: {landmark.x}, y: {landmark.y}, z: {landmark.z})")
        
            # Print nose landmark.
        image_hight, image_width, _ = frame.shape
        x=(int(results.pose_landmarks.landmark[mp_pose.PoseLandmark.RIGHT_WRIST].x * image_width))
        y=(int(results.pose_landmarks.landmark[mp_pose.PoseLandmark.RIGHT_WRIST].y * image_hight))
        
        Allpoints.append(Point(x,y,1))
        x=(int(results.pose_landmarks.landmark[mp_pose.PoseLandmark.LEFT_WRIST].x * image_width))
        y=(int(results.pose_landmarks.landmark[mp_pose.PoseLandmark.LEFT_WRIST].y * image_hight))
        
        Allpoints.append(Point(x,y,1))

        if framecnt%30==0:
              framecnt=0
              #print (Allpoints)
              result = recognizer.recognize(Allpoints)
              gesture_name, score = result
              print (result)
              Allpoints.clear()  
        
        mp_drawing.draw_landmarks(frame, results.pose_landmarks, mp_pose.POSE_CONNECTIONS)
        # show the final output
        cv2.imshow('Output', frame)
        if score>0.3:
            # Send gesture to app.py through a socket connection
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                try:
                    s.connect(('localhost', 12345))
                    s.sendall(f"GESTURE:{result}".encode('utf-8'))
                except Exception as e:
                    print("Error sending gesture data:", e,f"GESTURE:{result}".encode('utf-8'))
        
    except:
            #break
            print ('Camera Error')
    if cv2.waitKey(1) == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()

