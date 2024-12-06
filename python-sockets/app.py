import socket
import time
import struct
import threading
import queue
from queue import Empty
import bluetooth
import subprocess
import Face_Recognition
import Emotion_Recognition

def scan_bluetooth_devices(queue):
    print("Starting continuous Bluetooth scan...")

    while True:
        try:
            # Discover nearby Bluetooth devices
            nearby_devices = bluetooth.discover_devices(lookup_names=True)
            # Add the latest scan results to the queue
            queue.put(nearby_devices)
            # Short delay before the next scan to avoid excessive looping
            time.sleep(1)

        except Exception as e:
            print(f"An error occurred during Bluetooth scan: {str(e)}")
            time.sleep(1)


def send_message(connection, message):
    message_bytes = message.encode("utf-8")
    message_length = struct.pack(">I", len(message_bytes))
    connection.sendall(message_length + message_bytes)
    connection.recv(1)  # Expect a single-byte acknowledgment


def send_data(connection, loggedInUserId, question, answers, image_paths , bluetooth_devices=[], gesture_data=None, flag=False):
    send_message(connection, f"ID:{loggedInUserId}")
    for q in question:
        send_message(connection, f"Q:{q}")
    for answer in answers:
        send_message(connection, f"A:{answer}")
    for img_path in image_paths:
        send_message(connection, f"IMG:{img_path}")
    for addr, name in bluetooth_devices:
        send_message(connection, f"BT:{addr},{name}")
        flag = True
    if gesture_data:
        send_message(connection, f"GESTURE:{gesture_data}")
    for correct_answer in correct_answers:
        send_message(connection, f"CORRECT_ANSWER:{correct_answer}")

    print("All data sent.")
    return flag


from queue import Empty

def login():
    users = [(1, "bin\Debug\person.jpg")]
    usersImgEncodings = Face_Recognition.createImageEncodings(users)
    mathchIndex = Face_Recognition.login(usersImgEncodings)
    return mathchIndex

def start_client(queue):
    flag = False
    loggedInUserId = login()
    if loggedInUserId is not None:
        while True:
            try:
                client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                client_socket.connect(("localhost", 12345))
            
                questions_easy = [
                    "What is the capital of Egypt?", 
                    "What animal lays eggs?", 
                    "What is the largest ocean on Earth?", 
                    "What is 2 + 2?", 
                    "Which color is the sky?"
                ]
                questions_medium = [
                    "Which animal can fly?", 
                    "What do plants need to grow?", 
                    "How many continents are there?", 
                    "What is the longest river in the world?", 
                    "What is the chemical symbol for water?"
                ]
                questions_hard = [
                    "What do bees make?", 
                    "What is the biggest mountain in the world?", 
                    "What is the powerhouse of the cell?", 
                    "What is the largest continent by area?", 
                    "What is the atomic number of Carbon?"
                ]

                answers_easy = [
                    ["Aswan", "Cairo", "Giza", "Behira"], 
                    ["Cow", "Chicken", "Fox", "Dog"], 
                    ["Atlantic", "Pacific", "Indian", "Arctic"], 
                    ["3", "4", "5", "6"], 
                    ["Blue", "Red", "Green", "Yellow"]
                ]
                answers_medium = [
                    ["Cow", "Chicken", "Bird", "Bat"], 
                    ["Milk", "Water", "Sunlight", "Soil"], 
                    ["5", "6", "7", "8"], 
                    ["Nile River", "Amazon River", "Yangtze River", "Mississippi River"], 
                    ["H2O", "CO2", "O2", "N2"]
                ]
                answers_hard = [
                    ["Honey", "Milk", "Bread", "Juice"], 
                    ["Mount Everest", "K2", "Kangchenjunga", "Mount Kilimanjaro"], 
                    ["Nucleus", "Mitochondria", "Ribosome", "Golgi apparatus"], 
                    ["Africa", "Asia", "North America", "Europe"], 
                    ["6", "8", "12", "14"]
                ]

                correct_answers_easy = [
                    "Cairo", "Chicken", "Pacific", "4", "Blue"
                ]
                correct_answers_medium = [
                    "Bird", "Sunlight", "7", "Nile River", "H2O"
                ]
                correct_answers_hard = [
                    "Honey", "Mount Everest", "Mitochondria", "Asia", "6"
                ]

                
                # Updated image paths corresponding to the correct answers
                image_paths_easy = [
                    "bin\\Debug\\cairo.jpg", "bin\\Debug\\chicken.jpg", "bin\\Debug\\Pacific_ocean.jpg", "bin\\Debug\\four.jpg", "bin\\Debug\\Blue.jpg"
                ]
                image_paths_medium = [
                    "bin\\Debug\\bird.jpg", "bin\\Debug\\sunlight.jpg", "bin\\Debug\\seven.jpg", "bin\\Debug\\Nile.jpg", "bin\\Debug\\h2o.jpg"
                ]
                image_paths_hard = [
                    "bin\\Debug\\honey.jpg", "bin\\Debug\\Mount_Everest.jpg", "bin\\Debug\\Mitochondria.jpg", "bin\\Debug\\Asia.jpg", "bin\\Debug\\six.jpg"
                ]
                # Continuously listen for new Bluetooth devices in the queue
                while True:
                    try:
                        # Attempt to get Bluetooth devices without blocking
                        bluetooth_devices = queue.get_nowait()
                        # Send data with the latest Bluetooth information
                        send_data(
                            client_socket, loggedInUserId,
                            questions_easy + questions_medium + questions_hard, 
                            answers_easy + answers_medium + answers_hard, 
                            correct_answers_easy + correct_answers_medium + correct_answers_hard,
                            image_paths_easy + image_paths_medium + image_paths_hard, 
                            bluetooth_devices, flag=flag
                        )
                        if(flag): break
                    except Empty:
                        send_data(
                            client_socket, loggedInUserId,
                            questions_easy + questions_medium + questions_hard, 
                            answers_easy + answers_medium + answers_hard, 
                            correct_answers_easy + correct_answers_medium + correct_answers_hard,
                            image_paths_easy + image_paths_medium + image_paths_hard,
                            flag=flag
                        )
                        if(flag): break
                    time.sleep(0.5)
            except Exception as e:
                print(f"Connection failed: {e}")
                time.sleep(2)
            finally:
                if flag: break
                client_socket.close()
    else:
        print("No user recognized or login process terminated.")

def run_gesture2_detection():
    # Run your gesture detection script
    gesture_script = "media-pipe.py"
    subprocess.Popen(["python", gesture_script])

def run_gesture_detection():
    # Run your gesture detection script
    gesture_script = "live_ges.py"
    subprocess.Popen(["python", gesture_script])
def run_reactivision():
    # Assuming reacTIVision is an executable or script
    reactivision_executable = "C:/Users/KamalM12/Vscode/Hci Project/reacTIVision-1.5.1-win64/reacTIVision.exe"
    subprocess.Popen([reactivision_executable]) 

def main():
    # Queue for sharing data between Bluetooth scanning and client sending
    bluetooth_queue = queue.Queue()
    
    # Start the gesture recognition script
    #gesture_thread = threading.Thread(target=run_gesture_detection)
    reactivision_thread = threading.Thread(target=run_reactivision)
    gesture2_thread = threading.Thread(target=run_gesture2_detection)
    #gesture_thread.start()
    reactivision_thread.start()
    gesture2_thread.start()
    #gesture_thread.join()
    reactivision_thread.join()
    gesture2_thread.join()

    # Start the socket client in a separate thread
    client_thread = threading.Thread(target=start_client, args=(bluetooth_queue,))
    client_thread.start()

   
    bluetooth_thread = threading.Thread(
        target=scan_bluetooth_devices, args=(bluetooth_queue,)
    )
    bluetooth_thread.start()

    # # Wait for threads to complete
    #bluetooth_thread.join()
    client_thread.join()

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        # close all threads and kill appplication
        exit(0)