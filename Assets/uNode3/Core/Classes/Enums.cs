namespace MaxyGames.UNode {
	public enum InfoType {
		None,
		Info,
		Warning,
		Error
	}

	public enum GenerationKind {
		Default,
		Performance,
		Compatibility,
	}

	public enum CompilationMethod {
		Unity,
		Roslyn,
	}

	public enum TypeDataKind : byte {
		Type,
		uNodeType,
		uNodeGenericType,
	}

	public enum SerializedTypeKind {
		None,
		Native,
		Runtime,
		GenericParameter,
	}

	public enum RefKind {
		None,
		Ref,
		Out,
		In,
	}

	public enum ArithmeticType : byte {
		Add,
		Subtract,
		Divide,
		Multiply,
		Modulo,
	}

	public enum ComparisonType : byte {
		Equal,
		NotEqual,
		LessThan,
		GreaterThan,
		LessThanOrEqual,
		GreaterThanOrEqual,
	}

	public enum ShiftType : byte {
		LeftShift,
		RightShift,
	}

	public enum BitwiseType : byte {
		And,
		Or,
		ExclusiveOr,
	}

	public enum OperatorKind {
		/// <summary>
		/// The == operator.
		/// </summary>
		Equality,
		/// <summary>
		/// The != operator.
		/// </summary>
		Inequality,
		/// <summary>
		/// The + operator.
		/// </summary>
		Addition,
		/// <summary>
		/// The - operator.
		/// </summary>
		Subtraction,
		/// <summary>
		/// The * operator.
		/// </summary>
		Multiply,
		/// <summary>
		/// The / operator.
		/// </summary>
		Division,
		/// <summary>
		/// The < operator.
		/// </summary>
		LessThan,
		/// <summary>
		/// The > operator.
		/// </summary>
		GreaterThan,
		/// <summary>
		/// The <= operator.
		/// </summary>
		LessThanOrEqual,
		/// <summary>
		/// The >= operator.
		/// </summary>
		GreaterThanOrEqual,
		/// <summary>
		/// The % operator.
		/// </summary>
		Modulus,
		/// <summary>
		/// The >> operator.
		/// </summary>
		RightShift,
		/// <summary>
		/// The << operator.
		/// </summary>
		LeftShift,
		/// <summary>
		/// The & operator.
		/// </summary>
		BitwiseAnd,
		/// <summary>
		/// The | operator.
		/// </summary>
		BitwiseOr,
		/// <summary>
		/// The ^ operator.
		/// </summary>
		ExclusiveOr,
		/// <summary>
		/// The ~ operator.
		/// </summary>
		BitwiseComplement,
		/// <summary>
		/// The && operator.
		/// </summary>
		LogicalAnd,
		/// <summary>
		/// The || operator.
		/// </summary>
		LogicalOr,
		/// <summary>
		/// The ! operator.
		/// </summary>
		LogicalNot
	}

	public enum SetType : byte { Change, Add, Subtract, Divide, Multiply, Modulo }

	public enum PortAccessibility : byte { ReadWrite, ReadOnly, WriteOnly }

	public enum SearchKind {
		Contains,
		Equals,
		Endswith,
		Startwith,
	}

	public enum PropertyAccessorKind : byte {
		ReadWrite,
		ReadOnly,
		WriteOnly,
	}

	public enum JumpStatementType {
		None,
		Continue,
		Break,
		Return,
	}

	public enum StateType {
		Success,
		Running,
		Failure,
	}

	public enum PortKind {
		FlowInput,
		FlowOutput,
		ValueInput,
		ValueOutput,
	}

	public enum DisplayKind {
		Normal,
		Partial,
		Full,
	}

	public enum GraphLayout {
		Vertical,
		Horizontal,
	}

	//[System.Flags]
	//public enum GraphTypeAttribute {
	//	NonPublic = 0,
	//	Public = 0 << 1,
	//	Abstract = 0 << 2,
	//	Sealed = 0 << 3,
	//	Class = 0 << 4,
	//	Interface = 0 << 5,
	//}
}
