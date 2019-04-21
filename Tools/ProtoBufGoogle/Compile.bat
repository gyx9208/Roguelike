protoc.exe Common.proto --csharp_out=./../../Assets/Scripts/Net/Proto
protoc.exe NetProto.proto --csharp_out=./../../Assets/Scripts/Net/Proto

GenCommandMap --csharp_out=./../../Assets/Scripts/Net/Proto

pause