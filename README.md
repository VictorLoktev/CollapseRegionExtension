# Collapse/Expand Region Extension 2022

### Collapse/Expand all #region-#endregion and ///\<summary>-\</summary> in the code editor window

***

Works with
Visual Studio **2022** **x64**
* Microsoft.VisualStudio.Community
* Microsoft.VisualStudio.Pro

https://github.com/VictorLoktev/CollapseRegionExtension

***

For Visual Studio **2019** use
https://github.com/Vlad-Herus/CollapseRegionExtension


***

## `#region - #endregion`


Supports '#region', '#pragma region' and '<!--region'.


RegionManagement.ExpandAll expands all *#region*s.\
Default keys are **Cotrol**+**Num+**.

RegionManagement.CollapseAll collapses all *#region*s.\
Default keys are **Cotrol**+**Num-**.

RegionManagement.ExpandCurrent expands *#region* under cursor.\
Default keys are **Cotrol**+**Shift**+**Num+**.

RegionManagement.CollapseCurrent collapses one *#region with caret in it.\
Default keys are **Cotrol**+**Shift**+**Num-**.

RegionManagement.ExpandAllButCurrent expands all *#region*s except one under cursor.\
Default keys are **Shift**+**Num+**.

RegionManagement.CollapseAllButCurrent collapses all *#region*s except one with caret in it.\
Default keys are **Shift**+**Num-**.

***

## `/// <summary> - </summary>`

RegionManagement.ExpandAllSummary expands all xml-comments with <summary> tag.\
Default keys are **Control**+**R** **Control**+**Num+**.

RegionManagement.CollapseAllSummary collapses all xml-comments with <summary> tag.\
Default keys are **Control**+**R** **Control**+**Num-**.

***

Default keys can be changed in Tools->Options->Environment->Keyboard with '**RegionManagement**'.


***

Original idea and code by Volodymyr Herus come from
https://github.com/VladimirUAZ/CollapseRegionExtension


