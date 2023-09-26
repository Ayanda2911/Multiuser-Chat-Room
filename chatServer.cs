// Ayanda Mcanyana
// Chat Room Assignment 

using System;
using System.Net;
using System.Net.Sockets; 
using System.Threading.Tasks;


public class Program {
	// 0 is reserved for the server 
	private static int connectedClients = 0; 
	
	public static void perClientChatServer(TcpClient acceptedClient){ 
		// From Client task 
		// get access to stream 
		var clientId = connectedClients;
		// get access to the stream 
		var stream = acceptedClient.GetStream();
		// initiliase bailout 
		var bailout = false; 
		
		var toClient = new StreamWriter(stream){
					AutoFlush = true
			};
		var fromClient = new StreamReader(stream); 
		var ToClientTask = Task.Run(() =>{
			MessageQueue.Enqueue($"Client {clientId:D} has joined the chat.",0); 
			
			var lastSeenMessageTime = MessageQueue.Now; 
			toClient.WriteLine("Welcome to the Chat Room");
			
			while(!bailout){
				// received message  
				var receivedMessage = MessageQueue.Dequeue(ref lastSeenMessageTime, clientId, in bailout);
				toClient.WriteLine(receivedMessage); 
			}
			
		}); 
		
		var FromClientTask = Task.Run(() =>{
			// while we are receiving messages and there is no bailout 
			while(!bailout){
				//invoke async method 
				var getMessage =  fromClient.ReadLineAsync();
				//wait for 20s to pass or read something from console
				var timeout = Task.WaitAny(getMessage, Task.Delay(20000));
			
				// if you the client didn't type something 
				if (timeout == 1){ 
					// enqueue message from server indicating that it wil be terminated
					toClient.WriteLine("Your session will be terminated");
					// enqueue a message "from server" indicating that the client will be terminated 
					MessageQueue.Enqueue($"Client {clientId:D} has stopped participating", 0);
					//signal to the "to client task to complete and wait "
					bailout = true; 
					ToClientTask.Wait();
					// enqueue for all the other users to see who has left 
					MessageQueue.Enqueue($"Client {clientId:D} has left the chat", 0);	
					acceptedClient.Close();
				}
				// we read something within 20s and get the result of the task 
				var msg = getMessage.Result;
				// if the message is null 
				if (msg == null){
					// signal the toclient task to complete
					toClient.WriteLine("Your session will be terminated");
					bailout = true; 
					ToClientTask.Wait();
					acceptedClient.Close();
				}
				// enqueue the message to for 
				MessageQueue.Enqueue(msg, clientId); 	
			} 
			
		});	
	// wait for all tasks to complete
	 Task.WaitAll(FromClientTask,ToClientTask); 
	}


	public static void Main (string[] args){
		//takes in command line arguments 
		var port = Int32.Parse(args[1]) ;
		// create a listener 
		var listener = new TcpListener(IPAddress.Any, port); 
		// start listening 
		listener.Start(); 
		// Tell server that its listening for clients
		Console.WriteLine("Listening for clients");
		
		if(args.Length > 2 ){
			MessageQueue.Show = true; 
		}
		// while the chatroom is active
		while(true){
			// accept tcp clients 
			// wait for a client to connect
			TcpClient client = listener.AcceptTcpClient();
			// launch per-client chat server 
			var y = Task.Factory.StartNew(obj => perClientChatServer((TcpClient)obj!), client);
			// interlocked increment to make it threadsafe 
			Interlocked.Increment(ref connectedClients);  		
		}	
	} 
} 

