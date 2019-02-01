for %%i in (*.proto) do (protogen +langver=3.0 --csharp_out=./../../Assets/Scripts/Net/Proto %%i)

GenCommandMap --csharp_out=./../../Assets/Scripts/Net/Proto

pause