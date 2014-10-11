MassiveNet
==========

Unity3d UDP networking library focused on high-CCU, multi-server architecture.

MassiveNet will be somewhat familiar to those who have used Unity's built-in networking or uLink.

Some of its features and design goals include:

  Actor-like messaging and synchronization via NetViews.  
  Easy RPC definition using the [NetRPC] attribute.  
  Automatic network LOD/culling for NetViews, a crucial feature for large CCU games.  
  Build large, open worlds with support for seamless client/NetView movement from server to server.  
  Network instantiation of NetViews through tagged prefabs. (@Owner, @Proxy, @Peer, @Creator)  
  Out-of-the-box serialization for common C# and Unity struct types.  
  Supports serialization of custom types via delegate registration.  
  Incremental synchronization of NetViews to avoid resource spiking.  

Getting Started
===========

View the GitHub wiki for an overview of MassiveNet's core concepts, then check out the examples.  
  
To get up and running quickly, grab the latest unitypackage from the release page:  
https://github.com/jakevn/MassiveNet/releases

Contribution
===========

Community participation wanted! 

Please submit ideas, bug reports, etc. to the issue tracker or submit a pull request.
