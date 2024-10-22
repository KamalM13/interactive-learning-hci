import socket
import time

def send_data(connection, question, answers, image_paths):
    # Send the question
    connection.sendall(question.encode('utf-8'))
    connection.recv(1024)  # Wait for acknowledgment

    # Send the answers
    for answer in answers:
        connection.sendall(answer.encode('utf-8'))
        connection.recv(1024)  # Wait for acknowledgment

    # Send the image paths
    for img_path in image_paths:
        connection.sendall(img_path.encode('utf-8'))
        connection.recv(1024)  # Wait for acknowledgment

    print("All data sent.")

def start_client():
    while True:
        try:
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client_socket.connect(('localhost', 12345))  # Ensure this is correct

                    # Define the question, answers, and image paths
            question = "What is the capital of Egypt?"
            answers = ["Cairo", "Aswan", "Giza", "Behira"]
            image_paths = ["cairo.png", "aswan.png", "giza.png", "behira.png"]

            send_data(client_socket, question, answers, image_paths)
            time.sleep(5)  # Pause for a while before sending again (optional)
        except Exception as e:
            print(f"Connection failed: {e}")
            time.sleep(2)  # Wait before trying to reconnect
        finally:
            client_socket.close()

if __name__ == "__main__":
    start_client()
