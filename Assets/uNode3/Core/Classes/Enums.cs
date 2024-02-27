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

	public enum SerialiedTypeKind {
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
		Default,
		Partial,
		Full,
	}

	public enum GraphLayout {
		Vertical,
		Horizontal,
	}
}
