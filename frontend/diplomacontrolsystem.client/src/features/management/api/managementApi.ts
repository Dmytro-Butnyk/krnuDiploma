import { apiRequest } from '../../../shared/api/client'
import type { EntityId } from '../../groups/api/types'
import type {
  AcademicDegreeDto,
  SecretaryDto,
  SpecialtyDto,
  TeacherDto,
  TeacherPositionDto,
  UpsertAcademicDegreeRequest,
  UpsertSecretaryRequest,
  UpsertSpecialtyRequest,
  UpsertTeacherPositionRequest,
  UpsertTeacherRequest,
} from './types'

export function getAcademicDegrees() {
  return apiRequest<AcademicDegreeDto[]>('/api/management/academic-degrees')
}

export function createAcademicDegree(request: UpsertAcademicDegreeRequest) {
  return apiRequest<AcademicDegreeDto>('/api/management/academic-degrees', {
    method: 'POST',
    body: request,
  })
}

export function updateAcademicDegree(degreeId: EntityId, request: UpsertAcademicDegreeRequest) {
  return apiRequest<AcademicDegreeDto>(`/api/management/academic-degrees/${degreeId}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteAcademicDegree(degreeId: EntityId) {
  return apiRequest<void>(`/api/management/academic-degrees/${degreeId}`, {
    method: 'DELETE',
  })
}

export function restoreAcademicDegree(degreeId: EntityId) {
  return apiRequest<AcademicDegreeDto>(`/api/management/academic-degrees/${degreeId}/restore`, {
    method: 'POST',
  })
}

export function getTeacherPositions() {
  return apiRequest<TeacherPositionDto[]>('/api/management/teacher-positions')
}

export function createTeacherPosition(request: UpsertTeacherPositionRequest) {
  return apiRequest<TeacherPositionDto>('/api/management/teacher-positions', {
    method: 'POST',
    body: request,
  })
}

export function updateTeacherPosition(positionId: EntityId, request: UpsertTeacherPositionRequest) {
  return apiRequest<TeacherPositionDto>(`/api/management/teacher-positions/${positionId}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteTeacherPosition(positionId: EntityId) {
  return apiRequest<void>(`/api/management/teacher-positions/${positionId}`, {
    method: 'DELETE',
  })
}

export function restoreTeacherPosition(positionId: EntityId) {
  return apiRequest<TeacherPositionDto>(`/api/management/teacher-positions/${positionId}/restore`, {
    method: 'POST',
  })
}

export function getSpecialties() {
  return apiRequest<SpecialtyDto[]>('/api/management/specialties')
}

export function createSpecialty(request: UpsertSpecialtyRequest) {
  return apiRequest<SpecialtyDto>('/api/management/specialties', {
    method: 'POST',
    body: request,
  })
}

export function updateSpecialty(specialtyId: EntityId, request: UpsertSpecialtyRequest) {
  return apiRequest<SpecialtyDto>(`/api/management/specialties/${specialtyId}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteSpecialty(specialtyId: EntityId) {
  return apiRequest<void>(`/api/management/specialties/${specialtyId}`, {
    method: 'DELETE',
  })
}

export function restoreSpecialty(specialtyId: EntityId) {
  return apiRequest<SpecialtyDto>(`/api/management/specialties/${specialtyId}/restore`, {
    method: 'POST',
  })
}

export function getSecretaries() {
  return apiRequest<SecretaryDto[]>('/api/management/secretaries')
}

export function createSecretary(request: UpsertSecretaryRequest) {
  return apiRequest<SecretaryDto>('/api/management/secretaries', {
    method: 'POST',
    body: request,
  })
}

export function updateSecretary(secretaryId: EntityId, request: UpsertSecretaryRequest) {
  return apiRequest<SecretaryDto>(`/api/management/secretaries/${secretaryId}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteSecretary(secretaryId: EntityId) {
  return apiRequest<void>(`/api/management/secretaries/${secretaryId}`, {
    method: 'DELETE',
  })
}

export function restoreSecretary(secretaryId: EntityId) {
  return apiRequest<SecretaryDto>(`/api/management/secretaries/${secretaryId}/restore`, {
    method: 'POST',
  })
}

export function hardDeleteSecretary(secretaryId: EntityId) {
  return apiRequest<void>(`/api/management/secretaries/${secretaryId}/hard-delete`, {
    method: 'DELETE',
  })
}

export function setSuperSecretaryRole(secretaryId: EntityId, isSuperSecretary: boolean) {
  return apiRequest<SecretaryDto>(`/api/management/secretaries/${secretaryId}/super-role`, {
    method: 'PATCH',
    body: { isSuperSecretary },
  })
}

export function getTeachers(specialtyId?: EntityId) {
  return apiRequest<TeacherDto[]>('/api/management/teachers', {
    query: { specialtyId },
  })
}

export function createTeacher(request: UpsertTeacherRequest) {
  return apiRequest<TeacherDto>('/api/management/teachers', {
    method: 'POST',
    body: request,
  })
}

export function updateTeacher(teacherId: EntityId, request: UpsertTeacherRequest) {
  return apiRequest<TeacherDto>(`/api/management/teachers/${teacherId}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteTeacher(teacherId: EntityId) {
  return apiRequest<void>(`/api/management/teachers/${teacherId}`, {
    method: 'DELETE',
  })
}

export function restoreTeacher(teacherId: EntityId) {
  return apiRequest<TeacherDto>(`/api/management/teachers/${teacherId}/restore`, {
    method: 'POST',
  })
}
