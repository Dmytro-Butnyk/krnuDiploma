import type { EntityId, PersonNameFormsDto } from '../../groups/api/types'

export interface AcademicDegreeDto {
  id: EntityId
  fullName: string
  shortName: string
  genitiveFullName: string
  genitiveShortName: string
  isActive: boolean
}

export interface TeacherPositionDto {
  id: EntityId
  fullName: string
  shortName: string
  genitiveFullName: string
  genitiveShortName: string
  isActive: boolean
}

export interface SpecialtyDto {
  id: EntityId
  code: string
  name: string
  isActive: boolean
}

export interface SecretaryDto {
  id: EntityId
  email: string
  fullName: string
  specialtyId: EntityId
  specialtyName: string
  isActive: boolean
  isSuperSecretary: boolean
  isGoogleLinked: boolean
}

export interface TeacherDto {
  id: EntityId
  fullName: string
  shortName: string
  nameForms: PersonNameFormsDto
  email: string
  phoneNumber: string
  academicDegreeId: EntityId
  academicDegree: string
  teacherPositionId: EntityId
  teacherPosition: string
  specialtyId: EntityId
  specialty: string
  isActive: boolean
}

export interface UpsertAcademicDegreeRequest {
  fullName: string
  shortName: string
  genitiveFullName: string | null
  genitiveShortName: string | null
  isActive: boolean | null
}

export type UpsertTeacherPositionRequest = UpsertAcademicDegreeRequest

export interface UpsertSpecialtyRequest {
  code: string
  name: string
  isActive: boolean | null
}

export interface UpsertSecretaryRequest {
  email: string
  fullName: string
  specialtyId: EntityId
  isActive: boolean
  isSuperSecretary: boolean
}

export interface UpsertTeacherRequest {
  fullName: string
  shortName: string
  nameForms?: PersonNameFormsDto | null
  email: string
  phoneNumber: string
  academicDegreeId: EntityId
  teacherPositionId: EntityId
  specialtyId: EntityId
  isActive: boolean | null
}
