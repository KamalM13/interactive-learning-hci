import face_recognition
import cv2
import time

def login(usersImgEncodings):
    ids = [userId for userId, encoding in usersImgEncodings]
    encodings = [encoding for userId, encoding in usersImgEncodings]

    video_capture = cv2.VideoCapture(0)    
    # Record the start time for time out
    start_time = time.time()  
    
    while True:
        
        elapsed_time = time.time() - start_time
        if elapsed_time > 30:
            print("Login timed out.")
            break
        
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to capture video frame.")
            break
        
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        
        face_locations = face_recognition.face_locations(rgb_frame)
        
        if not face_locations:
            continue 
        
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