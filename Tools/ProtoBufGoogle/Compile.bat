protoc.exe Common.proto --csharp_out=./../../Assets/MoleMole/Data/Proto
protoc.exe Client.proto --csharp_out=./../../Assets/MoleMole/Data/Proto
protoc.exe Dispatch.proto --csharp_out=./../../Assets/MoleMole/Data/Proto
protoc.exe NetProto.proto --csharp_out=./../../Assets/MoleMole/Data/Proto

GenCommandMap --csharp_out=./../../Assets/MoleMole/Data/Proto

pause