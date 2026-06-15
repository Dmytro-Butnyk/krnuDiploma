import { apiRequest } from '../../../shared/api/client'
import type { EducationLevel, EntityId } from '../../groups/api/types'
import type {
  CommissionHeadDto,
  CreateCommissionHeadRequest,
  CreateDiplomaExaminationCommissionRequest,
  DiplomaExaminationCommissionResponse,
  GetDiplomaExaminationCommissionOptionsResponse,
  UpdateCommissionHeadRequest,
  UpdateDiplomaExaminationCommissionRequest,
} from './types'

function withoutSecretaryEmail<T extends { secretaryEmail?: string }>(request: T): Omit<T, 'secretaryEmail'> {
  const payload = { ...request }
  delete payload.secretaryEmail

  return payload
}

export function getDiplomaExaminationCommissions(
  _secretaryEmail: string,
  educationLevel: EducationLevel,
  defenseYear: string,
) {
  void _secretaryEmail

  return apiRequest<DiplomaExaminationCommissionResponse>('/api/diploma-examination-commissions', {
    query: {
      EducationLevel: educationLevel,
      DefenseYear: defenseYear,
    },
  })
}

export function getDiplomaExaminationCommissionOptions(
  _secretaryEmail: string,
  _commissionId?: EntityId,
) {
  void _secretaryEmail
  void _commissionId

  return apiRequest<GetDiplomaExaminationCommissionOptionsResponse>(
    '/api/diploma-examination-commissions/options',
  )
}

export function getCommissionHeads(_secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<CommissionHeadDto[]>('/api/commission-heads')
}

export function createCommissionHead(request: CreateCommissionHeadRequest) {
  return apiRequest<CommissionHeadDto>('/api/commission-heads', {
    method: 'POST',
    body: withoutSecretaryEmail(request),
  })
}

export function updateCommissionHead(commissionHeadId: EntityId, request: UpdateCommissionHeadRequest) {
  return apiRequest<CommissionHeadDto>(`/api/commission-heads/${commissionHeadId}`, {
    method: 'PUT',
    body: withoutSecretaryEmail(request),
  })
}

export function deleteCommissionHead(commissionHeadId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<void>(`/api/commission-heads/${commissionHeadId}`, {
    method: 'DELETE',
  })
}

export function createDiplomaExaminationCommission(request: CreateDiplomaExaminationCommissionRequest) {
  return apiRequest<DiplomaExaminationCommissionResponse>('/api/diploma-examination-commissions', {
    method: 'POST',
    body: withoutSecretaryEmail(request),
  })
}

export function updateDiplomaExaminationCommission(
  commissionId: EntityId,
  request: UpdateDiplomaExaminationCommissionRequest,
) {
  return apiRequest<DiplomaExaminationCommissionResponse>(
    `/api/diploma-examination-commissions/${commissionId}`,
    {
      method: 'PUT',
      body: withoutSecretaryEmail(request),
    },
  )
}

export function deleteDiplomaExaminationCommission(commissionId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<void>(`/api/diploma-examination-commissions/${commissionId}`, {
    method: 'DELETE',
  })
}
