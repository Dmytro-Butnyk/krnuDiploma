import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../../shared/api/client'
import type {
  ApiId,
  GenerateDocumentRequest,
  GetTemplateDetailsResponse,
  ScanTemplateForTagsResponse,
  TemplateListItemDto,
  UpdateTemplatePayload,
  UploadTemplatePayload,
  UploadTemplateResponse,
} from '../model/types'

export const templatesQueryKey = ['documents', 'templates'] as const

export async function fetchTemplates() {
  const response = await apiClient.get<TemplateListItemDto[]>('/api/documents/templates')
  return response.data
}

export async function fetchTemplateDetails(id: ApiId) {
  const response = await apiClient.get<GetTemplateDetailsResponse>(`/api/documents/templates/${id}`)
  return response.data
}

export async function scanTemplateForTags(template: File) {
  const formData = new FormData()
  formData.append('template', template)

  const response = await apiClient.post<ScanTemplateForTagsResponse>('/api/documents/scan', formData)
  return response.data
}

export async function uploadTemplate(payload: UploadTemplatePayload) {
  const formData = new FormData()
  formData.append('name', payload.name)
  formData.append('configurationJson', payload.configurationJson)
  formData.append('template', payload.template)

  const response = await apiClient.post<UploadTemplateResponse>('/api/documents/templates', formData)
  return response.data
}

export async function updateTemplate(payload: UpdateTemplatePayload) {
  const formData = new FormData()
  formData.append('templateId', String(payload.templateId))
  formData.append('name', payload.name ?? '')
  formData.append('configurationJson', payload.configurationJson ?? '')
  formData.append('template', payload.template ?? '')

  const response = await apiClient.patch<ApiId>(
    `/api/documents/templates/${payload.templateId}`,
    formData,
  )
  return response.data
}

export async function deleteTemplate(id: ApiId) {
  const response = await apiClient.delete<ApiId>(`/api/documents/templates/${id}`)
  return response.data
}

export async function generateDocument(id: ApiId, payload: GenerateDocumentRequest) {
  const response = await apiClient.post<Blob>(`/api/documents/${id}/generate`, payload, {
    responseType: 'blob',
  })
  return response.data
}

export function useTemplates() {
  return useQuery({
    queryKey: templatesQueryKey,
    queryFn: fetchTemplates,
  })
}

export function useTemplateDetails(id: ApiId) {
  return useQuery({
    queryKey: [...templatesQueryKey, id],
    queryFn: () => fetchTemplateDetails(id),
    enabled: Boolean(id),
  })
}

export function useScanTemplateForTags() {
  return useMutation({
    mutationFn: scanTemplateForTags,
  })
}

export function useUploadTemplate() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: uploadTemplate,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: templatesQueryKey })
    },
  })
}

export function useUpdateTemplate() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: updateTemplate,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: templatesQueryKey })
    },
  })
}

export function useDeleteTemplate() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteTemplate,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: templatesQueryKey })
    },
  })
}

export function useGenerateDocument() {
  return useMutation({
    mutationFn: ({ id, parameters }: { id: ApiId; parameters: Record<string, string> }) =>
      generateDocument(id, { parameters }),
  })
}
