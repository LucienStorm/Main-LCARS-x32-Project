' LCARSbuttons/LCARSbuttons/Diagnostics/IEngineeringPanelRefresh.vb
Option Strict On
Option Explicit On

Namespace Diagnostics

''' <summary>Called when an Engineering accordion section expands so panels can sync live state.</summary>
Public Interface IEngineeringPanelRefresh
    Sub OnSectionShown()
End Interface

End Namespace
