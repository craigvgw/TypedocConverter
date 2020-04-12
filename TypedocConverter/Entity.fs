﻿module Entity

type Entity =
| TypeEntity of
    Id: int *
    Name: string *
    InnerTypes: Entity list
| TypeParameterEntity of
    Id: int *
    Name: string // TODO: Add contraints support
| ParameterEntity of
    Id: int *
    Name: string *
    Type: Entity
| ConstructorEntity of
    Id: int *
    Name: string *
    Comment: string *
    Modifiers: string list *
    Parameters: Entity list
| PropertyEntity of
    Id: int *
    Name: string *
    Comment: string *
    Modifiers: string list *
    Type: Entity *
    WithGet: bool *
    WithSet: bool *
    IsOptional: bool *
    InitialValue: string option
| EventEntity of
    Id: int *
    Name: string *
    Comment: string *
    Modifiers: string list *
    IsOptional: bool *
    Type: Entity
| EnumMemberEntity of
    Id: int *
    Name: string *
    Comment: string *
    Value: string option
| MethodEntity of
    Id: int *
    Name: string *
    Comment: string *
    Modifier: string list *
    TypeParameters: Entity list *
    Parameters: Entity list *
    ReturnType: Entity
| EnumEntity of 
    Id: int *
    Namespace: string *
    Name: string *
    Comment: string *
    Modifiers: string list *
    Members: Entity list
| ClassInterfaceEntity of
    Id: int *
    Namespace: string *
    Name: string *
    Comment: string *
    Modifiers: string list *
    Members: Entity list *
    InheritedFroms: Entity list *
    TypeParameters: Entity list *
    IsInterface: bool
| StringUnionEntity of
    Id: int *
    Namespace: string *
    Name: string *
    Comment: string *
    Modifiers: string list *
    Members: Entity list
| TypealiasEntity of // not used
    Id: int *
    AliasedType: Entity
| UnionTypeEntity of // not used
    Id: int *
    Types: Entity list