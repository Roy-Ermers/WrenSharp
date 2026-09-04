using WrenSharp;
using WrenSharp.Interop;


var content = File.ReadAllText("./code.wren");

var config = new WrenVMConfiguration()
{
    LogErrors = true,
    ModuleProvider = new ModuleProvider()
};


using var vm = new WrenSharpVM(config);
Binding.Bind(vm);

vm.Interpret(
    module: "main",
    source: content, 
    throwOnFailure: true
    );


class ModuleProvider : IWrenModuleProvider
{
    public IWrenSource GetModuleSource(WrenVM vm, string module)
    {
        return new WrenStringSource("""
                                    foreign class Vector3 {
                                        construct new() {}
                                        construct new(x,y,z) {}
                                        foreign print()
                                    }                                    
                                    foreign class logger {
                                        foreign static log(message)
                                        foreign static error(message)
                                    }
                                    """);
    }

    public void OnModuleLoadComplete(WrenVM vm, string module, IWrenSource source)
    {
       
    }
}