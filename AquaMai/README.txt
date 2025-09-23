由于AquaMai的DLL中，assembly是编码起来的而不是直接暴露符号，因此无法通过添加依赖DLL的方式直接调用。
故这里封装一些对AquaMai里面的：
- 通过反射的方式调用内容；
- 个别工具性质的、依赖不复杂的东西，也可能会直接复制代码，在StarrahMai的assembly里自己编译一份出来。