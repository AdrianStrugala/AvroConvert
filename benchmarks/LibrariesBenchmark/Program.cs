using BenchmarkDotNet.Running;

if (args is ["smoke"])
{
    Smoke.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
