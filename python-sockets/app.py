import socket
import time
import struct
import threading
import queue
from queue import Empty
#import bluetooth
import subprocess

def scan_bluetooth_devices(queue):
    print("Starting continuous Bluetooth scan...")

    while True:
        try:
            # Discover nearby Bluetooth devices
            #nearby_devices = bluetooth.discover_devices(lookup_names=True)
            # Add the latest scan results to the queue
            #queue.put(nearby_devices)
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


def send_data(connection, question, answers, image_paths, bluetooth_devices=[], gesture_data=None, flag = False):
    send_message(connection, f"Q:{question}")
    for answer in answers:
        send_message(connection, f"A:{answer}")
    for img_path in image_paths:
        send_message(connection, f"IMG:{img_path}")
    for addr, name in bluetooth_devices:
        send_message(connection, f"BT:{addr},{name}")
    if gesture_data:
        send_message(connection, f"GESTURE:{gesture_data}")
        #flag = True
    print("All data sent.")
    return flag 

from queue import Empty

def start_client(queue):
    while True:
        try:
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client_socket.connect(("localhost", 12345))

            # Define the question, answers, and image paths
            question = ["What is the capital of Egypt?", "What animal lays eggs?"]
            answers = ["Aswan", "Cairo", "Giza", "Behira", "Chicken", "Cow", "Dog", "Fox"]
            image_paths = ["cairo.jpg", "aswan.jpg", "giza.jpg", "behira.jpg", "chicken.jpg", "cow.jpg","dog.jpg","fox.jpg"]
            flag = True
            # Continuously listen for new Bluetooth devices in the queue
            while True:
                try:
                    # Attempt to get Bluetooth devices without blocking
                    bluetooth_devices = queue.get_nowait()
                    # Send data with the latest Bluetooth information
                    send_data(
                        client_socket, question, answers, image_paths, bluetooth_devices, flag
                    )
                except Empty:
                    if(flag): break
                    # Queue is empty, proceed without sending Bluetooth data
                    send_data(client_socket, question, answers, image_paths)
                # Short delay to prevent tight-looping
                time.sleep(0.5)
        except Exception as e:
            print(f"Connection failed: {e}")
            time.sleep(2)
        finally:
            if flag: break
            client_socket.close()



def main():
    # Queue for sharing data between Bluetooth scanning and client sending
    bluetooth_queue = queue.Queue()
    
    # Start the gesture recognition script
    live="F:/Uni/4th year/Hci/project/interactive-learning-hci/python-sockets/live_ges.py"
    gesture_process = subprocess.Popen(["python", live])

    # Start the socket client in a separate thread
    client_thread = threading.Thread(target=start_client, args=(bluetooth_queue,))
    client_thread.start()

    # Start Bluetooth scanning in a separate thread
    bluetooth_thread = threading.Thread(
        target=scan_bluetooth_devices, args=(bluetooth_queue,)
    )
    bluetooth_thread.start()

    # Wait for threads to complete
    bluetooth_thread.join()
    client_thread.join()
    # Optionally, wait for the gesture process to complete
    #gesture_process.wait()  # This will wait for live_ges.py to finish if needed


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        # close all threads and kill appplication
        exit(0)