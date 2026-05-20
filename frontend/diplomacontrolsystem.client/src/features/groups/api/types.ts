export type EntityId = number | string
export type EducationLevel = 'Bachelor' | 'Master'

export interface GroupDto {
  id: EntityId
  name: string
}

export interface AcademicYearOverviewResponse {
  year: string
  defenseYear: string
  groups: GroupDto[]
}

export interface PhysicalChecklistDto {
  hasStudentCard: boolean
  hasGradeBook: boolean
  hasCircular: boolean
  hasSignedReview: boolean
  hasCopyOfBankReceipt: boolean
  hasExplanatoryNote: boolean
}

export interface ElectronicChecklistDto {
  hasRegulatoryControl: boolean
  hasExplanatoryNoteDoc: boolean
  hasExplanatoryNotePdf: boolean
  hasPlagiarismReportPdf: boolean
  hasReviewDoc: boolean
  hasPresentationPpt: boolean
}

export interface GroupStudentResponse {
  id: EntityId
  fullName: string
  supervisorName: string | null
  physicalChecklist: PhysicalChecklistDto | null
  electronicChecklist: ElectronicChecklistDto | null
}

export interface CreateGroupRequest {
  secretaryEmail: string
  name: string
  year: string
  educationLevel: EducationLevel
  studentsFile?: File | null
  googleDriveUrl?: string | null
}

export interface CreateGroupResponse {
  groupId: EntityId
  name: string
  year: string
  defenseYear: string
  educationLevel: string
  studentsCreated: EntityId
}

export interface UpdateGroupRequest {
  secretaryEmail: string
  name: string | null
  year: string | null
  educationLevel: EducationLevel | null
}

export interface UpdateGroupResponse {
  id: EntityId
  name: string
  year: string
  defenseYear: string
  educationLevel: string
}

export interface AddStudentRequest {
  secretaryEmail: string
  lastName: string
  firstName: string
  middleName: string
}

export interface AddStudentResponse {
  studentId: EntityId
  fullName: string
  groupId: EntityId
}

export interface StatisticItemDto {
  key: string
  label: string
  count: EntityId
  percentage: number | string
}

export interface StatisticSectionDto {
  key: string
  title: string
  items: StatisticItemDto[]
}

export interface GroupStatisticsResponse {
  groupId: EntityId
  groupName: string
  totalStudents: EntityId
  sections: StatisticSectionDto[]
}

export interface StudentNameDto {
  lastName: string
  firstName: string
  middleName: string
}

export interface QualificationWorkDto {
  topic: string
  supervisorId: EntityId | null
  supervisorName: string | null
  practiceBase: string
  reviewerId: EntityId | null
  reviewerName: string | null
}

export interface DefenceInfoDto {
  defenceDate: string | null
}

export interface DefenceResultsDto {
  plagiarismPercent: number | string
  uniquePercent: number | string
  supervisorScore: EntityId
  reviewerScore: EntityId
  commissionScore: EntityId
  ectsGrade: string
  nationalGrade: string
  hasDiplomaWithHonors: boolean
}

export interface CharacteristicsDto {
  isResearchBased: boolean
  hasRealProjects: boolean
  isEcoFriendly: boolean
  isEnterpriseOrdered: boolean
  isComplexInteruniversity: boolean
  isComplexInterdepartmental: boolean
  isComplexDepartmental: boolean
  isComplexProjectParticipant: boolean
  isRecommendedForMaster: boolean
  isRecommendedForImplementation: boolean
  isDefendedAtEnterprise: boolean
}

export interface StudentDetailsResponse {
  id: EntityId
  groupId: EntityId
  fullName: string
  name: StudentNameDto
  qualificationWork: QualificationWorkDto | null
  physicalChecklist: PhysicalChecklistDto | null
  electronicChecklist: ElectronicChecklistDto | null
  defenceInfo: DefenceInfoDto | null
  defenceResults: DefenceResultsDto | null
  characteristics: CharacteristicsDto | null
}

export interface TeacherOptionDto {
  id: EntityId
  fullName: string
  shortName: string
}

export interface QualificationWorkOptionsResponse {
  supervisors: TeacherOptionDto[]
  reviewers: TeacherOptionDto[]
}

export type UpdateStudentNameRequest = AddStudentRequest

export interface UpdateStudentQualificationWorkRequest {
  secretaryEmail: string
  topic: string
  supervisorId: EntityId | null
  practiceBase: string
  reviewerId: EntityId | null
}

export interface UpdateStudentDefenceRequest {
  secretaryEmail: string
  defenceDate: string | null
}

export type UpdatePhysicalChecklistRequest = PhysicalChecklistDto & { secretaryEmail: string }
export type UpdateElectronicChecklistRequest = ElectronicChecklistDto & { secretaryEmail: string }
export type UpdateDefenceResultsRequest = DefenceResultsDto & { secretaryEmail: string }
export type UpdateQualificationWorkCharacteristicsRequest = CharacteristicsDto & { secretaryEmail: string }
