using Xunit;

namespace FlexRadioServices.Tests.Hardware;

/// <summary>
/// Defines the non-parallel test collection that owns FlexLib static state.
/// </summary>
[CollectionDefinition("Hardware", DisableParallelization = true)]
public sealed class HardwareCollection;
