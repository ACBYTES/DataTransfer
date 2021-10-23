# DEVELOPED BY [ACBYTES (ALIREZA SHAHBAZI) - (HTTPS://WWW.ACBYTES.IR)]
(! Everything works on one single stream, thus, locking stream writes are necessary to avoid data loss as chunks are read in a pre-calculated order. On the other hand, packet counts aren't guaranteed !)
(! For a bit less work, there are no end-of-file commands sent and when the whole file is received, the corresponding ClientFile confirms its chunk and both sides dispose their IFile objects !)
(! Space-separated path handling has not been implemented, Use -explorer for the ease of use !)
(! Client side doesn't check for timeouts !)
(! Can handle multiple files at the same time !)
(! Connection failures or timeouts will lead to cancellations that if needed, could be replaced by retrials !)
(! [Server.cs.220] The attempt for checking the available physical memory is somehow dummy. This has been done to just make sure there's enough memory for each chunk at construction but no functionalities on the server or client side have been implemented to deal with memory shortages, meaning that if either of the sides run out of memory during execution, chunks won't be read/sent/received in smaller sizes and thus, 'OutOfMemoryException's are possible.
   This workflow was not intended for this project !)
(! If there's an active client connected and a new client gets connected to the server as well, the success message will be shown to the new user although the server does only accept one client at a time so the connection will be queued on the server side. No implementation has been done to approve pending connections as it was not intended for this project !)
Workflow:
- (Starting as a server or as a client)
{
	- Server (Waits for a client) => (Notifies about the first connection) => (Sends a file with a header containing file's name length, content length and file's name) => (Waits for client's confirmation $ After the timeout, the operation will be cancelled. $) <=
	- Client (Connects to a server) => (Waits for incoming files) => (Parses headers and receives data) => (Confirms the received chunks and waits for the rest of the chunks.) <=
}
