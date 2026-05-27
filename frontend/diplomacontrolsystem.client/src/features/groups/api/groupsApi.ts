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
  QualificationWorkOptionsResponse,
  StudentDetailsResponse,
  UpdateGroupRequest,
  UpdateGroupResponse,
  UpdateDefenceResultsRequest,
  UpdateElectronicChecklistRequest,
  UpdatePhysicalChecklistRequest,
  UpdateQualificationWorkCharacteristicsRequest,
  UpdateStudentDefenceRequest,
  UpdateStudentNameRequest,
  UpdateStudentQualificationWorkRequest,
} from './types'

export function getAcademicYears(secretaryEmail: string, educationLevel: EducationLevel) {
  return apiRequest<AcademicYearOverviewResponse[]>('/api/groups/academic-years', {
    query: {
      SecretaryEmail: secretaryEmail,
      EducationLevel: educationLevel,
    },
  })
}

export function getGroupStudents(groupId: EntityId, secretaryEmail: string) {
  return apiRequest<GroupStudentResponse[]>(`/api/groups/${groupId}/students`, {
    query: { secretaryEmail },
  })
}

export function getGroupStatistics(groupId: EntityId, secretaryEmail: string) {
  return apiRequest<GroupStatisticsResponse>(`/api/groups/${groupId}/statistics`, {
    query: { secretaryEmail },
  })
}

export function createGroup(request: CreateGroupRequest) {
  const formData = new FormData()
  formData.set('secretaryEmail', request.secretaryEmail)
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
  formData.set('secretaryEmail', request.secretaryEmail)

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
    body: request,
  })
}

export function deleteGroup(groupId: EntityId, secretaryEmail: string) {
  return apiRequest<void>(`/api/groups/${groupId}`, {
    method: 'DELETE',
    query: { secretaryEmail },
  })
}

export function addStudent(groupId: EntityId, request: AddStudentRequest) {
  return apiRequest<AddStudentResponse>(`/api/groups/${groupId}/students`, {
    method: 'POST',
    body: request,
  })
}

export function deleteStudent(studentId: EntityId, secretaryEmail: string) {
  return apiRequest<void>(`/api/students/${studentId}`, {
    method: 'DELETE',
    query: { secretaryEmail },
  })
}

export function getStudentDetails(studentId: EntityId, secretaryEmail: string) {
  return apiRequest<StudentDetailsResponse>(`/api/students/${studentId}/details`, {
    query: { secretaryEmail },
  })
}

export function getQualificationWorkOptions(studentId: EntityId, secretaryEmail: string) {
  return apiRequest<QualificationWorkOptionsResponse>(
    `/api/students/${studentId}/qualification-work-options`,
    {
      query: { secretaryEmail },
    },
  )
}

export function updateStudentName(studentId: EntityId, request: UpdateStudentNameRequest) {
  return apiRequest(`/api/students/${studentId}/name`, {
    method: 'PATCH',
    body: request,
  })
}

export function updateStudentQualificationWork(
  studentId: EntityId,
  request: UpdateStudentQualificationWorkRequest,
) {
  return apiRequest(`/api/students/${studentId}/qualification-work`, {
    method: 'PATCH',
    body: request,
  })
}

export function updatePhysicalChecklist(studentId: EntityId, request: UpdatePhysicalChecklistRequest) {
  return apiRequest(`/api/students/${studentId}/physical-checklist`, {
    method: 'PATCH',
    body: request,
  })
}

export function updateElectronicChecklist(studentId: EntityId, request: UpdateElectronicChecklistRequest) {
  return apiRequest(`/api/students/${studentId}/electronic-checklist`, {
    method: 'PATCH',
    body: request,
  })
}

export function updateStudentDefence(studentId: EntityId, request: UpdateStudentDefenceRequest) {
  return apiRequest(`/api/students/${studentId}/defence`, {
    method: 'PATCH',
    body: request,
  })
}

export function updateDefenceResults(studentId: EntityId, request: UpdateDefenceResultsRequest) {
  return apiRequest(`/api/students/${studentId}/defence-results`, {
    method: 'PATCH',
    body: request,
  })
}

export function updateQualificationWorkCharacteristics(
  studentId: EntityId,
  request: UpdateQualificationWorkCharacteristicsRequest,
) {
  return apiRequest(`/api/students/${studentId}/qualification-work-characteristics`, {
    method: 'PATCH',
    body: request,
  })
}
