#!/usr/bin/env dotnet

#:property PublishAot=false
#:property Configuration=Release
#:property Optimize=true

#:package BenchmarkDotNet@0.15.8

#pragma warning disable CA1822 Mark members as static
#pragma warning disable CA1050 Declare types in namespaces

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;


var summary = BenchmarkRunner.Run<Benchmarks>();


[InProcess]
public class Benchmarks
{
    [Benchmark]
    public void Benchmark()
    {
    }
}
