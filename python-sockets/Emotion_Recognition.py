from deepface import DeepFace
import cv2

def emotionDetection():
    video_capture = cv2.VideoCapture(1)

    if not video_capture.isOpened():
        print("Error: Could not access the camera.")
        return

    print("Starting emotion detection... Press 'q' to exit.")
    
    while True:
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to capture video frame.")
            break

        try:
            # Analyze the frame for emotions
            result = DeepFace.analyze(frame, actions=["emotion"], enforce_detection=False)
            emotions = result[0]["emotion"]
            dominant_emotion = result[0]["dominant_emotion"]

            # Draw the emotion details on the frame
            draw_emotion_details(frame, emotions, dominant_emotion)
        except Exception as e:
            print(f"Emotion detection failed: {e}")

        # Display the frame with the emotion overlay
        cv2.imshow("Emotion Detection", frame)

        # Exit when 'q' is pressed
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    # Release resources
    video_capture.release()
    cv2.destroyAllWindows()
    print("Emotion detection ended.")

def draw_emotion_details(frame, emotions, dominant_emotion):
    """
    Draws emotion analysis on the frame.
    """
    height, width, _ = frame.shape

    # Position and colors
    text_color = (255, 255, 255)
    bar_color = (0, 255, 0)
    bar_width = 200
    x_offset = 10
    y_offset = 50
    bar_height = 20
    spacing = 10

    # Overlay dominant emotion text
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

    # Draw bars for each emotion
    for emotion, confidence in emotions.items():
        confidence_bar_length = int((confidence / 100) * bar_width)
        label = f"{emotion}: {confidence:.1f}%"

        # Draw background bar
        cv2.rectangle(
            frame,
            (x_offset, y_offset),
            (x_offset + bar_width, y_offset + bar_height),
            (50, 50, 50),
            -1
        )

        # Draw confidence bar
        cv2.rectangle(
            frame,
            (x_offset, y_offset),
            (x_offset + confidence_bar_length, y_offset + bar_height),
            bar_color,
            -1
        )

        # Draw the label
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

if __name__ == "__main__":
    emotionDetection()