$password = cat C:\MyDoc\defSPNPassword.txt | convertto-securestring
$username = '2d03d99e-e95a-4655-8e8c-9a5f9f406ab6'
$cred = new-object System.Management.Automation.PSCredential($username, $password)

Login-AzureRMAccount -Credential $cred -ServicePrincipal -Tenant '72f988bf-86f1-41af-91ab-2d7cd011db47'
Select-AzureRmSubscription -SubscriptionName "TK pltkw3" -Tenant '72f988bf-86f1-41af-91ab-2d7cd011db47'

New-AzureRmResourceGroupDeployment -Force -Name pltkw3sfclusterDepl -ResourceGroupName pltkw3sfcluster `
-TemplateFile "azuredeploy.json" `
-existingVMSSName n1 -newCapacity 6

pause