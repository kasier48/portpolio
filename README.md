# 프로젝트 서명
다른 유저와 멀티가 가능한 디펜스 장르의 프로젝트를 개인적으로 개발하고 있습니다.
이에 관련된 코드를 포트폴리오로 삼고자 합니다.
해당 코드들은 일부 코드들만 포함되어 있음을 참고 부탁드립니다.
폴더 기준으로 대략적인 설명 다음과 같습니다.

## 1) AOI
액터 간의 시야 처리를 위함 입니다.
3x3 행렬을 기준으로 해서 시야 처리를 합니다.
AoiGrid를 통해 나의 시야에서 벗어나거나 추가되거나 하는 액터들을 추가하고 삭제합니다.
AreaOfInterest는 나의 시야에 포함된 액터들에 대한 데이터들을 관리 합니다.
Multicast를 할 떄 AreaOfInterest을 기준으로 처리합니다.

## 2) Collision
액터간의 충돌 처리를 위함 입니다.
액터 마다 CylinderColider를 갖고 있고 해당 Colider를 이용해서 액터간의 충돌 처리를 판정 합니다.
다른 액터 간의 충돌 처리 기준은 CollisionGrid를 통해 주변 액터들을 가져와 충돌 처리를 합니다.

## 3) Component
액터는 컴포넌트 단위의 개념으로 로직이 실행 됩니다.
MoveComponent는 이동과 관련된 컴포넌트 입니다.
MoveComponent의 Move가 실행되는 시점은 ActorMoveState이라는 이동 상태에서 처리 됩니다.
NavigatorComponent는 액터가 지형과 충돌 혹은 액터간의 충돌 시 회피하여 목적지에 도착해야 하는 경우에 사용합니다.
현재는 AStart 기반의 길 찾기를 통해 회피하고 있지만, 차후 가시 그래프를 이용한 개념으로 바꾸어 효율적인 길 찾기를 할 예정 입니다.
AStart 기반이다 보니 작은 cell 단위의 길 찾기는 목적지가 멀수록 무수히 많은 경로 계산이 필요하기 때문에 가시 그래프 개념으로 바꿀 예정 입니다.
다만, 액터간의 충돌 시에는 AStart를 이용해서 액터를 회피 한 이후 가시 그래프를 이용해서 다시 목적지로 갈 계획 입니다.

## 4) Fsm
액터들의 상태는 상태머신으로 관리 되어집니다.
상태 머신을 통해서 Idle -> Move 혹은 Move -> Attack 등으로 상태 이전이 가능합니다.
ActorMoveState는 이동 상태에 대한 베이스에 해당하는 로직을 처리 합니다.
ActorMonsterMoveState, ActorUserMoveState는 ActorMoveState을 상속 받아 이동 중에 장애물 혹은 액터 충돌 시에 대한 처리를 달리합니다.

## 5) World
Topography에서 cell 단위의 지형 정보를 메모리로 맵핑 하는 처리를 합니다.
Navigator는 cell 단위로 AStart 기반으로 길 찾기를 수행합니다.

## 6) Room
Room 단위로 프레임이 돌아가며, timingWheel에 의하여 프레임 틱은 호출 되어집니다.
Room은 WorkerJobDispatcher를 구현하여 로직에 대한 순차처리를 보장합니다.




 

