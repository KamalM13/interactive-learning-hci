import cv2
import numpy as np
cap = cv2.VideoCapture(0)

if not cap.isOpened():
    print("Error: Could not access the camera.")
    exit()

while True:
    ret, frame = cap.read()
    if not ret:
        print("Error: Could not read frame.")
        break

    hsv_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)

    lower_red = np.array([125, 50, 50])  #for mobile flashlight
    upper_red = np.array([160, 255, 255]) #for mobile flashlight
        # Create a mask for the laser
    mask = cv2.inRange(hsv_frame, lower_red, upper_red)

        # Apply morphological operations to clean up the mask
    mask = cv2.erode(mask, None, iterations=2)
    mask = cv2.dilate(mask, None, iterations=2)

        # Find contours in the mask
    contours, _ = cv2.findContours(mask.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    black_screen = np.zeros_like(frame)

    if len(contours) > 0:
            # Find the largest contour
        largest_contour = max(contours, key=cv2.contourArea)
          # Minimum area threshold # Calculate the center of the contour
        M = cv2.moments(largest_contour)
        if M["m00"] > 0:
            cx = int(M["m10"] / M["m00"])
            cy = int(M["m01"] / M["m00"])
            cv2.circle(black_screen, (cx, cy), 10, (255, 0, 0), -1)
        
    cv2.imshow('Camera Feed', frame)
    cv2.imshow('Laser Pointer Tracking', black_screen)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()