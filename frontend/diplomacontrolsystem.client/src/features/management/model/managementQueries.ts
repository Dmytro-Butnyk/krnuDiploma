import { queryOptions } from '@tanstack/react-query'
import type { EntityId } from '../../groups/api/types'
import {
  getAcademicDegrees,
  getSecretaries,
  getSpecialties,
  getTeacherPositions,
  getTeachers,
} from '../api/managementApi'

export const managementQueryKeys = {
  all: ['management'] as const,
  academicDegrees: () => [...managementQueryKeys.all, 'academic-degrees'] as const,
  teacherPositions: () => [...managementQueryKeys.all, 'teacher-positions'] as const,
  specialties: () => [...managementQueryKeys.all, 'specialties'] as const,
  secretaries: () => [...managementQueryKeys.all, 'secretaries'] as const,
  teachers: (specialtyId?: EntityId) => [...managementQueryKeys.all, 'teachers', specialtyId ?? null] as const,
}

export function academicDegreesQuery() {
  return queryOptions({
    queryKey: managementQueryKeys.academicDegrees(),
    queryFn: getAcademicDegrees,
  })
}

export function teacherPositionsQuery() {
  return queryOptions({
    queryKey: managementQueryKeys.teacherPositions(),
    queryFn: getTeacherPositions,
  })
}

export function specialtiesQuery() {
  return queryOptions({
    queryKey: managementQueryKeys.specialties(),
    queryFn: getSpecialties,
  })
}

export function secretariesQuery() {
  return queryOptions({
    queryKey: managementQueryKeys.secretaries(),
    queryFn: getSecretaries,
  })
}

export function teachersQuery(specialtyId?: EntityId) {
  return queryOptions({
    queryKey: managementQueryKeys.teachers(specialtyId),
    queryFn: () => getTeachers(specialtyId),
  })
}
