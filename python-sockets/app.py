import socket
import time
import struct
import asyncio
from bleak import BleakScanner

async def scan_bluetooth_devices(queue):
    print("Starting continuous Bluetooth scan...")

    while True:
        try:
            # Discover nearby Bluetooth devices
            devices = await BleakScanner.discover()
            bluetooth_devices = [
                {"name": device.name or "Unknown", "address": device.address}
                for device in devices
            ]
            
            # Add the latest scan results to the queue
            await queue.put(bluetooth_devices)
            
            # Display scan results in the console for reference
            print(f"Found {len(bluetooth_devices)} devices:")
            for device in bluetooth_devices:
                print(f"Device Name: {device['name']}, MAC Address: {device['address']}")
            
            # Short delay before the next scan to avoid excessive looping
            await asyncio.sleep(1)

        except Exception as e:
            print(f"An error occurred during Bluetooth scan: {str(e)}")
            await asyncio.sleep(1)

def send_message(connection, message):
    message_bytes = message.encode('utf-8')
    message_length = struct.pack('>I', len(message_bytes))
    connection.sendall(message_length + message_bytes)
    connection.recv(1)  # Expect a single-byte acknowledgment

def send_data(connection, question, answers, image_paths, bluetooth_devices):
    send_message(connection, f"Q:{question}")
    for answer in answers:
        send_message(connection, f"A:{answer}")
    for img_path in image_paths:
        send_message(connection, f"IMG:{img_path}")
    for device in bluetooth_devices:
        send_message(connection, f"{device['address']}")
    print("All data sent.")

def start_client(queue):
    while True:
        try:
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client_socket.connect(('localhost', 12345))

            # Define the question, answers, and image paths
            question = "What is the capital of Egypt?"
            answers = ["Aswan", "Cairo", "Giza", "Behira"]
            image_paths = ["cairo.jpg", "aswan.jpg", "giza.jpg", "behira.jpg"]

            # Continuously listen for new Bluetooth devices in the queue
            while True:
                try:
                    bluetooth_devices = queue.get_nowait()
                    send_data(client_socket, question, answers, image_paths, bluetooth_devices)
                except asyncio.QueueEmpty:
                    # If queue is empty, skip sending data
                    time.sleep(0.5)

        except Exception as e:
            print(f"Connection failed: {e}")
            time.sleep(2)
        finally:
            client_socket.close()

async def main():
    queue = asyncio.Queue()

    # Run Bluetooth scanning as a coroutine and start_client in a thread
    tasks = [
        asyncio.create_task(scan_bluetooth_devices(queue)),  # Bluetooth scan
        asyncio.to_thread(start_client, queue)               # Socket client in a separate thread
    ]

    try:
        await asyncio.gather(*tasks)
    except KeyboardInterrupt:
        print("Keyboard interrupt received, cancelling tasks...")
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)
    finally:
        print("Program terminated.")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("Program terminated by user.")
