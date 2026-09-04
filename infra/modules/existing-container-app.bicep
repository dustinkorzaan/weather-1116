// Reads back the deploy-owned state (image, environment variables, secrets) of a
// container app that already exists.
//
// An ARM/Bicep deployment is a PUT, so every property the template hardcodes
// wins over whatever was set out of band. The container apps here are created by
// infra/main.bicep but their image, deploy-time env vars, and secrets belong to
// .github/workflows/prod-deploy-*.yml, so provision has to hand the live values
// back instead of reasserting the placeholder image and a bare env list.
//
// A Bicep `existing` reference only resolves for a resource that is actually
// there, which is why the caller passes an explicit exists flag rather than this
// module discovering it: infra/scripts/capture-existing-container-apps.sh
// computes the flags before `azd provision` runs.

@description('Container app name to look up, e.g. wx1116-prod-api.')
param name string

@description('False before the app exists (first provision), true afterwards.')
param exists bool

resource containerApp 'Microsoft.App/containerApps@2024-03-01' existing = if (exists) {
  name: name
}

// The ! assertions are safe because ARM's if() only evaluates the taken branch,
// so nothing dereferences containerApp unless exists is true.

@description('Live image reference, or empty when the app does not exist yet.')
output image string = exists ? containerApp!.properties.template.containers[0].image : ''

@description('Live container env vars, or empty when the app does not exist yet.')
output env array = exists ? (containerApp!.properties.template.containers[0].env ?? []) : []

// Wrapped in an object because @secure() applies to string and object outputs only.
@secure()
@description('Live container app secrets as { list: [{ name, value }] }.')
output secrets object = {
  list: exists ? (containerApp!.listSecrets().value ?? []) : []
}
