$folders = Get-ChildItem -Path ./**/bin,./**/obj,./bin,./.vs -Directory;
$folders | Remove-ITem -Recurse -Force -Confirm:$false;