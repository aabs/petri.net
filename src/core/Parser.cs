namespace petrinets2.core;

public partial class Parser
{
	private readonly string _spec;

	public Parser(Scanner scanner)
	{
		_spec = scanner.Source;
		Builder = CreatePetriNet.Called("ParsedNet");
	}

	public CreatePetriNet Builder { get; }

	public ParserErrors errors { get; } = new ParserErrors();

	public void Parse()
	{
		// Keep legacy API surface intact. The previous generated parser/scanner
		// types are not present in this project, so this parser currently only
		// validates that a non-empty spec was supplied.
		if (string.IsNullOrWhiteSpace(_spec))
		{
			errors.count++;
		}
	}
}

public sealed class Scanner
{
	public Scanner(Stream stream)
	{
		using var reader = new StreamReader(stream, leaveOpen: true);
		Source = reader.ReadToEnd();
	}

	public string Source { get; }
}

public sealed class ParserErrors
{
	public int count { get; set; }
}

public sealed class ParserException : Exception
{
	public ParserException(ParserErrors errors)
		: base($"Parser failed with {errors.count} error(s).")
	{
	}
}