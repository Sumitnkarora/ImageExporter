param (
    [string]$dest = $pwd.Path
 );
 
$pathAndFileName = $(join-path -path $dest -childpath "buildinfo.xml")
$now = Get-Date
$values =@{}
$values.Add("project",$Env:BuildId);
$values.Add("build",$Env:BuildNumber);
$values.Add("revision",$Env:RevisionNumber);
$values.Add("timestamp",$now.ToString("MM/dd/yyyy hh:mm:ss tt"));
$values.Add("projectVersion",$Env:ProjectVersion);
 
If(!(test-path $dest))
{
    md $dest
}
 
 
[xml]$Doc = New-Object System.Xml.XmlDocument;
$root = $doc.CreateNode("element","buildInfo",$null);
 
 
Foreach ($key in $values.Keys){
   $null = $element = $doc.CreateNode("element",$key,$null);
   $null = $element.InnerText =    $values[$key];
   $null = $root.AppendChild($element);
}
 
$doc.AppendChild($root);
$doc.save($pathAndFileName);
 
"Build Info created: $pathAndFileName "