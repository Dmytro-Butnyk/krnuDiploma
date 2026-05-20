const secretaryEmailStorageKey = 'diploma-control-secretary-email'

export function getStoredSecretaryEmail() {
  return window.localStorage.getItem(secretaryEmailStorageKey) ?? ''
}

export function storeSecretaryEmail(secretaryEmail: string) {
  window.localStorage.setItem(secretaryEmailStorageKey, secretaryEmail)
}

export function clearStoredSecretaryEmail() {
  window.localStorage.removeItem(secretaryEmailStorageKey)
}
