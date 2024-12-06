import face_recognition
import cv2
import time

import cv2
import time
import face_recognition

def login(usersImgEncodings):
    # Extract user IDs and face encodings
    ids = [userId for userId, encoding in usersImgEncodings]
    encodings = [encoding for userId, encoding in usersImgEncodings]

    video_capture = cv2.VideoCapture(0)  # Open the camera feed
    start_time = time.time()  # Record the start time for timeout

    while True:
        elapsed_time = time.time() - start_time
        if elapsed_time > 30:  # Timeout after 30 seconds
            print("Login timed out.")
            break

        # Capture a single frame
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to capture video frame.")
            break

        # Display the camera feed
        cv2.imshow("Login Camera", frame)
        if cv2.waitKey(1) & 0xFF == ord('q'):  # Press 'q' to exit manually
            break

        # Convert the frame to RGB
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        # Detect face locations in the frame
        face_locations = face_recognition.face_locations(rgb_frame)

        if not face_locations:
            continue  # Skip to the next frame if no faces are detected

        # Find face encodings
        try:
            face_encodings = face_recognition.face_encodings(rgb_frame, face_locations)
        except Exception as e:
            continue

        # Compare against known encodings
        for face_encoding in face_encodings:
            matches = face_recognition.compare_faces(encodings, face_encoding)
            if True in matches:
                matched_index = matches.index(True)
                user_id = ids[matched_index]

                # Cleanup and return the matched user ID
                video_capture.release()
                cv2.destroyAllWindows()
                return user_id

    # Cleanup
    video_capture.release()
    cv2.destroyAllWindows()
    print("Login process ended without a match.")
    return None

def createImageEncodings(usersIdToImgPath):
    usersImgEncodings = []
    for user in usersIdToImgPath:
        userId, imgPath = user
        
        try:
            image = face_recognition.load_image_file(imgPath)
            encodings = face_recognition.face_encodings(image)
            
            if encodings:
                usersImgEncodings.append((userId, encodings[0]))
            else:
                print(f"No face found in image: {imgPath}")
        except Exception as e:
            print(f"Error processing image {imgPath}: {e}")
    
    return usersImgEncodings