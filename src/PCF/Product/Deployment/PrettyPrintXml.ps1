$children = Get-ChildItem Config/*.xml
foreach ($child in $children)
{
    $xml = [xml](gc $child)
    $xml.Save($child.FullName)
}