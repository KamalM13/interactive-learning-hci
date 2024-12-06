import socket
import time
import struct
import threading
import queue
from queue import Empty
import bluetooth
import subprocess
import Face_Recognition
import os

# import Emotion_Recognition


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


def send_data(
    connection,
    loggedInUserId,
    bluetooth_devices=[],
    gesture_data=None,
    flag=False,
):
    send_message(connection, f"ID:{loggedInUserId}")
    # for q in question:
    #     send_message(connection, f"Q:{q}")
    # for answer in answers:
    #     send_message(connection, f"A:{answer}")
    # for img_path in image_paths:
    #     send_message(connection, f"IMG:{img_path}")
    for addr, name in bluetooth_devices:
        send_message(connection, f"BT:{addr},{name}")
        flag = True
    if gesture_data:
        send_message(connection, f"GESTURE:{gesture_data}")
    # for correct_answer in correct_answers:
    #     send_message(connection, f"CORRECT_ANSWER:{correct_answer}")

    print("All data sent.")
    return flag


from queue import Empty


def login():
    users = [(1, "../bin/Debug/person.jpg")]
    usersImgEncodings = Face_Recognition.createImageEncodings(users)
    mathchIndex = Face_Recognition.login(usersImgEncodings)
    return mathchIndex


def start_client(queue):
    flag = False
    loggedInUserId = login()
    if loggedInUserId is not None:
        reactivision_thread = threading.Thread(target=run_reactivision)
        reactivision_thread.start()
        reactivision_thread.join()
        while True:
            try:
                client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                client_socket.connect(("localhost", 12345))
                # Continuously listen for new Bluetooth devices in the queue
                while True:
                    try:
                        # Attempt to get Bluetooth devices without blocking
                        bluetooth_devices = queue.get_nowait()
                        # Send data with the latest Bluetooth information
                        send_data(
                            client_socket,
                            loggedInUserId,
                            
                            bluetooth_devices,
                            flag=flag,
                        )
                        if flag:
                            break
                    except Empty:
                        send_data(
                            client_socket,
                            loggedInUserId,
                            flag=flag,
                        )
                        if flag:
                            break
                    time.sleep(0.5)
            except Exception as e:
                print(f"Connection failed: {e}")
                time.sleep(2)
            finally:
                if flag:
                    break
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
    reactivision_executable = (
        "C:/Users/KamalM12/Vscode/Hci Project/reacTIVision-1.5.1-win64/reacTIVision.exe"
    )
    subprocess.Popen([reactivision_executable])


def main():
    # Queue for sharing data between Bluetooth scanning and client sending
    bluetooth_queue = queue.Queue()

    # Start the gesture recognition script
    # gesture_thread = threading.Thread(target=run_gesture_detection)

    # gesture2_thread = threading.Thread(target=run_gesture2_detection)
    # gesture_thread.start()

    # gesture2_thread.start()
    # gesture_thread.join()

    # gesture2_thread.join()

    # Start the socket client in a separate thread
    client_thread = threading.Thread(target=start_client, args=(bluetooth_queue,))
    client_thread.start()

    bluetooth_thread = threading.Thread(
        target=scan_bluetooth_devices, args=(bluetooth_queue,)
    )
    bluetooth_thread.start()

    # # Wait for threads to complete
    # bluetooth_thread.join()
    client_thread.join()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        # close all threads and kill appplication
        print("Exiting...")
        os._exit(0)
