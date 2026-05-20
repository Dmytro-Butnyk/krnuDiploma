import { queryOptions } from '@tanstack/react-query'
import {
  getAcademicYears,
  getGroupStatistics,
  getGroupStudents,
  getQualificationWorkOptions,
  getStudentDetails,
} from '../api/groupsApi'
import type { EducationLevel, EntityId } from '../api/types'

export const groupsQueryKeys = {
  all: ['groups'] as const,
  academicYears: (secretaryEmail: string, educationLevel: EducationLevel) =>
    [...groupsQueryKeys.all, 'academic-years', secretaryEmail, educationLevel] as const,
  students: (groupId: EntityId, secretaryEmail: string) =>
    [...groupsQueryKeys.all, 'students', String(groupId), secretaryEmail] as const,
  statistics: (groupId: EntityId, secretaryEmail: string) =>
    [...groupsQueryKeys.all, 'statistics', String(groupId), secretaryEmail] as const,
  studentDetails: (studentId: EntityId, secretaryEmail: string) =>
    [...groupsQueryKeys.all, 'student-details', String(studentId), secretaryEmail] as const,
  qualificationWorkOptions: (studentId: EntityId, secretaryEmail: string) =>
    [...groupsQueryKeys.all, 'qualification-work-options', String(studentId), secretaryEmail] as const,
}

export function academicYearsQuery(secretaryEmail: string, educationLevel: EducationLevel) {
  return queryOptions({
    queryKey: groupsQueryKeys.academicYears(secretaryEmail, educationLevel),
    queryFn: () => getAcademicYears(secretaryEmail, educationLevel),
    enabled: secretaryEmail.length > 0,
  })
}

export function groupStudentsQuery(groupId: EntityId | undefined, secretaryEmail: string) {
  return queryOptions({
    queryKey: groupsQueryKeys.students(groupId ?? 'missing', secretaryEmail),
    queryFn: () => getGroupStudents(groupId ?? '', secretaryEmail),
    enabled: Boolean(groupId) && secretaryEmail.length > 0,
  })
}

export function groupStatisticsQuery(groupId: EntityId | undefined, secretaryEmail: string) {
  return queryOptions({
    queryKey: groupsQueryKeys.statistics(groupId ?? 'missing', secretaryEmail),
    queryFn: () => getGroupStatistics(groupId ?? '', secretaryEmail),
    enabled: Boolean(groupId) && secretaryEmail.length > 0,
  })
}

export function studentDetailsQuery(studentId: EntityId | undefined, secretaryEmail: string) {
  return queryOptions({
    queryKey: groupsQueryKeys.studentDetails(studentId ?? 'missing', secretaryEmail),
    queryFn: () => getStudentDetails(studentId ?? '', secretaryEmail),
    enabled: Boolean(studentId) && secretaryEmail.length > 0,
  })
}

export function qualificationWorkOptionsQuery(studentId: EntityId | undefined, secretaryEmail: string) {
  return queryOptions({
    queryKey: groupsQueryKeys.qualificationWorkOptions(studentId ?? 'missing', secretaryEmail),
    queryFn: () => getQualificationWorkOptions(studentId ?? '', secretaryEmail),
    enabled: Boolean(studentId) && secretaryEmail.length > 0,
  })
}
