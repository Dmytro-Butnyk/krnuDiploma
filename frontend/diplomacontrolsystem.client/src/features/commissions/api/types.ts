import type { EducationLevel, EntityId, GroupDto } from '../../groups/api/types'

export interface TeacherDto {
  id: EntityId
  fullName: string
  position: string
}

export interface SecretaryDto {
  id: EntityId
  fullName: string
}

export interface PersonDto {
  fullName: string
  position: string | null
}

export interface HeadDto {
  teacher: TeacherDto | null
  person: PersonDto | null
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
  head: HeadDto
  members: MemberDto[]
  secretary: SecretaryDto
  groups: GroupDto[]
}

export interface GetDiplomaExaminationCommissionOptionsResponse {
  groups: GroupDto[]
  teachers: TeacherDto[]
  secretaries: SecretaryDto[]
}

export interface CreateDiplomaExaminationCommissionRequest {
  secretaryEmail: string
  secretaryId: EntityId
  orderNumber: string
  educationLevel: EducationLevel
  defenseYear: string
  groupIds: EntityId[]
  headTeacherId: EntityId | null
  headPersonaName: string | null
  headPersonaPosition: string | null
  firstMemberTeacherId: EntityId
  secondMemberTeacherId: EntityId
  thirdMemberTeacherId: EntityId
  startDate: string
  endDate: string
}

export type UpdateDiplomaExaminationCommissionRequest = CreateDiplomaExaminationCommissionRequest
