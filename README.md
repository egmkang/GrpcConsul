# POC for Grpc + Consul

  * Server register a service in Consul
  * Client tries to invoke a service with Consul as resolver

# How this works ?  

Hook into CallInvoker and select random endpoint.

# Notice

This is a POC only. Do what you want with this !