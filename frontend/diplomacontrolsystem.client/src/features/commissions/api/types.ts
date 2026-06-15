import type { EducationLevel, EntityId, GroupDto, PersonNameFormsDto } from '../../groups/api/types'

export interface TeacherDto {
  id: EntityId
  fullName: string
  position: string
}

export interface SecretaryDto {
  id: EntityId
  fullName: string
}

export interface CommissionHeadDto {
  id: EntityId
  fullName: string
  nameForms: PersonNameFormsDto
  position: string
  company: string
  specialty: string
  isDeleted: boolean
}

export interface MemberDto {
  teacherId: EntityId
  fullName: string
  position: string
}

export interface DiplomaExaminationCommissionResponse {
  id: EntityId
  orderNumber: string
  educationLevel: EducationLevel
  year: string
  defenseYear: string
  startDate: string
  endDate: string
  head: CommissionHeadDto
  members: MemberDto[]
  secretary: SecretaryDto
  groups: GroupDto[]
}

export interface GetDiplomaExaminationCommissionOptionsResponse {
  teachers: TeacherDto[]
  secretaries: SecretaryDto[]
  commissionHeads: CommissionHeadDto[]
}

export interface CreateCommissionHeadRequest {
  secretaryEmail: string
  fullName: string
  nameForms?: PersonNameFormsDto | null
  position: string
  company: string
  specialty: string
}

export type UpdateCommissionHeadRequest = CreateCommissionHeadRequest

export interface CreateDiplomaExaminationCommissionRequest {
  secretaryEmail: string
  secretaryId: EntityId
  orderNumber: string
  educationLevel: EducationLevel
  defenseYear: string
  commissionHeadId: EntityId
  firstMemberTeacherId: EntityId
  secondMemberTeacherId: EntityId
  thirdMemberTeacherId: EntityId
  startDate: string
  endDate: string
}

export type UpdateDiplomaExaminationCommissionRequest = Omit<
  CreateDiplomaExaminationCommissionRequest,
  'educationLevel' | 'defenseYear'
>
