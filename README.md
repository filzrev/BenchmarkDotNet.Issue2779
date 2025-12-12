This repository is a minimal reproduction of a BenchmarkDotNet issue [#2779](https://github.com/dotnet/BenchmarkDotNet/issues/2779)

## 1. How to reproduce problem

1. Fork this repository
2. Enable GitHub Actions on forked repository
3. Run following command with GitHub CLI.
    ```
    gh workflow run run-tests.yaml --ref main -f runs_on=macos-latest -f configuration=Release -f targetFramework=net8.0 -f iterationCount=100
    ```
4. Confirm benchmark failed on random iteration.

## Problem details

When running `AllocationQuantumIsNotAnIssueForNetCore21Plus` benchmark.

`Allocated Bytes` is expected to be 88 bytes.
But it returns wrong value(424 byte) randomly.

Currently this issue is confirmed on macos(x64/arm64).
It's not occurred on other platforms.





