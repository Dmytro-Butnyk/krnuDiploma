import { apiRequest } from '../../../shared/api/client'
import type {
  AcademicYearOverviewResponse,
  AddStudentRequest,
  AddStudentResponse,
  CreateGroupRequest,
  CreateGroupResponse,
  EducationLevel,
  EntityId,
  GroupStatisticsResponse,
  GroupStudentResponse,
  ImportGroupDefenceResultsRequest,
  ImportGroupDefenceResultsResponse,
  ImportTableColumnsResponse,
  PracticeBaseRatingResponse,
  PreviousYearComparisonResponse,
  QualificationWorkOptionsResponse,
  SupervisorWorkloadResponse,
  StudentDetailsResponse,
  UpdateGroupRequest,
  UpdateGroupResponse,
  UpdateDefenceResultsRequest,
  UpdateDefenceQuestionsRequest,
  UpdateElectronicChecklistRequest,
  UpdatePhysicalChecklistRequest,
  UpdateQualificationWorkCharacteristicsRequest,
  UpdateStudentDefenceRequest,
  UpdateStudentNameRequest,
  UpdateStudentQualificationWorkRequest,
} from './types'

function withoutSecretaryEmail<T extends { secretaryEmail?: string }>(request: T): Omit<T, 'secretaryEmail'> {
  const payload = { ...request }
  delete payload.secretaryEmail

  return payload
}

export function getAcademicYears(_secretaryEmail: string, educationLevel: EducationLevel) {
  void _secretaryEmail

  return apiRequest<AcademicYearOverviewResponse[]>('/api/groups/academic-years', {
    query: {
      EducationLevel: educationLevel,
    },
  })
}

export function getGroupStudents(groupId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<GroupStudentResponse[]>(`/api/groups/${groupId}/students`)
}

export function getGroupStatistics(groupId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<GroupStatisticsResponse>(`/api/groups/${groupId}/statistics`)
}

export function getStudentImportColumns(_secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<ImportTableColumnsResponse>('/api/groups/student-import/columns')
}

export function getDefenceResultsImportColumns(_secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<ImportTableColumnsResponse>('/api/groups/defence-results/import/columns')
}

export function getPreviousYearComparison(groupId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<PreviousYearComparisonResponse>(`/api/groups/${groupId}/statistics/previous-year-comparison`)
}

export function getSupervisorWorkload(groupId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<SupervisorWorkloadResponse>(`/api/groups/${groupId}/statistics/supervisor-workload`)
}

export function getPracticeBaseRating(groupId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<PracticeBaseRatingResponse>(`/api/groups/${groupId}/statistics/practice-bases`)
}

export function createGroup(request: CreateGroupRequest) {
  const formData = new FormData()
  formData.set('name', request.name)
  formData.set('year', request.year)
  formData.set('educationLevel', request.educationLevel)

  if (request.studentsFile) {
    formData.append('studentsFile', request.studentsFile, request.studentsFile.name)
  }

  if (request.googleDriveUrl) {
    formData.set('googleDriveLink', request.googleDriveUrl)
    formData.set('googleDriveUrl', request.googleDriveUrl)
  }

  return apiRequest<CreateGroupResponse>('/api/groups', {
    method: 'POST',
    body: formData,
  })
}

export function importGroupDefenceResults(groupId: EntityId, request: ImportGroupDefenceResultsRequest) {
  const formData = new FormData()

  if (request.resultsFile) {
    formData.append('resultsFile', request.resultsFile, request.resultsFile.name)
  }

  if (request.googleDriveLink) {
    formData.set('googleDriveLink', request.googleDriveLink)
  }

  return apiRequest<ImportGroupDefenceResultsResponse>(`/api/groups/${groupId}/defence-results/import`, {
    method: 'POST',
    body: formData,
  })
}

export function updateGroup(groupId: EntityId, request: UpdateGroupRequest) {
  return apiRequest<UpdateGroupResponse>(`/api/groups/${groupId}`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function deleteGroup(groupId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<void>(`/api/groups/${groupId}`, {
    method: 'DELETE',
  })
}

export function addStudent(groupId: EntityId, request: AddStudentRequest) {
  return apiRequest<AddStudentResponse>(`/api/groups/${groupId}/students`, {
    method: 'POST',
    body: withoutSecretaryEmail(request),
  })
}

export function deleteStudent(studentId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<void>(`/api/students/${studentId}`, {
    method: 'DELETE',
  })
}

export function getStudentDetails(studentId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<StudentDetailsResponse>(`/api/students/${studentId}/details`)
}

export function getQualificationWorkOptions(studentId: EntityId, _secretaryEmail: string) {
  void _secretaryEmail

  return apiRequest<QualificationWorkOptionsResponse>(
    `/api/students/${studentId}/qualification-work-options`,
  )
}

export function updateStudentName(studentId: EntityId, request: UpdateStudentNameRequest) {
  return apiRequest(`/api/students/${studentId}/name`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updateStudentQualificationWork(
  studentId: EntityId,
  request: UpdateStudentQualificationWorkRequest,
) {
  return apiRequest(`/api/students/${studentId}/qualification-work`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updatePhysicalChecklist(studentId: EntityId, request: UpdatePhysicalChecklistRequest) {
  return apiRequest(`/api/students/${studentId}/physical-checklist`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updateElectronicChecklist(studentId: EntityId, request: UpdateElectronicChecklistRequest) {
  return apiRequest(`/api/students/${studentId}/electronic-checklist`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updateStudentDefence(studentId: EntityId, request: UpdateStudentDefenceRequest) {
  return apiRequest(`/api/students/${studentId}/defence`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updateDefenceResults(studentId: EntityId, request: UpdateDefenceResultsRequest) {
  return apiRequest(`/api/students/${studentId}/defence-results`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updateDefenceQuestions(studentId: EntityId, request: UpdateDefenceQuestionsRequest) {
  return apiRequest(`/api/students/${studentId}/qualification-work/defence-questions`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}

export function updateQualificationWorkCharacteristics(
  studentId: EntityId,
  request: UpdateQualificationWorkCharacteristicsRequest,
) {
  return apiRequest(`/api/students/${studentId}/qualification-work-characteristics`, {
    method: 'PATCH',
    body: withoutSecretaryEmail(request),
  })
}
