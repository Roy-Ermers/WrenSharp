using WrenSharp;
using WrenSharp.Interop;
using WrenTest;


var content = File.ReadAllText("./code.wren");

var config = new WrenVMConfiguration()
{
    LogErrors = false,
    WriteOutput = new WrenConsoleOutput(),
    ModuleProvider = new WrenBinding(),
};


using var vm = new WrenSharpVM(config);
WrenBinding.Bind(vm);

try
{
    vm.Interpret(
        module: "main",
        source: content,
        throwOnFailure: true
    );
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    throw;
}

