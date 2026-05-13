export type ApiId = number | string

export type ProblemDetails = {
  type?: string | null
  title?: string | null
  status?: number | string | null
  detail?: string | null
  instance?: string | null
}

export type HttpValidationProblemDetails = ProblemDetails & {
  errors?: Record<string, string[]>
}

export type GenerateDocumentRequest = {
  parameters: Record<string, string>
}

export type ScanTemplateForTagsResponse = {
  tags: string[]
}

export type TemplateListItemDto = {
  id: ApiId
  name: string
}

export type GetTemplateDetailsResponse = {
  id: ApiId
  name: string
  configurationJson: string | null
  requiredArguments: string[]
}

export type UploadTemplateResponse = {
  name: string
  templateId: ApiId
}

export type UploadTemplatePayload = {
  name: string
  configurationJson: string
  template: File
}

export type UpdateTemplatePayload = {
  templateId: ApiId
  name: string | null
  template: File | null
  configurationJson: string | null
}
