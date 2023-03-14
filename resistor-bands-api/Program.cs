using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ResistorBands>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Resistor Bands API", Version = "v1" });

    var locationOfExecutable = Assembly.GetExecutingAssembly().Location;
    var execFileNameWithoutExtension = Path.GetFileNameWithoutExtension(locationOfExecutable);
    var execFilePath = Path.GetDirectoryName(locationOfExecutable);
    var xmlFilePath = Path.Combine(execFilePath, $"{execFileNameWithoutExtension}.xml");
    o.IncludeXmlComments(xmlFilePath); 
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/colors", (ResistorBands resistorBands) => 
{
    return Results.Ok(resistorBands.Colors);
})
    .WithName("GetColors")
    .WithTags("Colors")
    .Produces<ResistorBands>(StatusCodes.Status200OK)
    .WithOpenApi(o => 
    {
        o.Summary = "Return all colors for bands on resistors";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "A list of all colors";

        return o;
    });

app.MapGet("/colors/{color}", (string color, ResistorBands resistorBands) => 
{
    var index = Array.IndexOf(resistorBands.Colors, color);
    if (index == -1 || index == 10 || index == 11)
    {
        return Results.NotFound();
    }
    var colorDetails = new ColorDetails(resistorBands.BandValues[index], resistorBands.Multipliers[index], resistorBands.Tolerances[index]);
    return Results.Ok(colorDetails);
})
    .WithName("GetColorDetails")
    .WithTags("Colors")
    .Produces<ColorDetails>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi(o => 
    {
        o.Summary = "Return details for a color band";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Details for a color band";
        o.Responses[((int)StatusCodes.Status404NotFound).ToString()].Description = "Unknown color";

        o.Parameters[0].Description = "Color for which to get details";

        return o;
    });

app.MapPost("resistors/value-from-bands", (ValueFromBandsReq valueFromBands, ResistorBands resistorBands) =>
{
    if (valueFromBands.FirstBand == null || valueFromBands.SecondBand == null || valueFromBands.Multiplier == null || valueFromBands.Tolerance == null)
    {
        return Results.BadRequest();
    }

    var firstBandIx = Array.IndexOf(resistorBands.Colors, valueFromBands.FirstBand);
    var secondBandIx = Array.IndexOf(resistorBands.Colors, valueFromBands.SecondBand);
    var toleranceIx = Array.IndexOf(resistorBands.Colors, valueFromBands.Tolerance);

    if (!resistorBands.Colors.Contains(valueFromBands.FirstBand)
    || firstBandIx == 10 || firstBandIx == 11
    || !resistorBands.Colors.Contains(valueFromBands.SecondBand)
    || secondBandIx == 10 || secondBandIx == 11
    || !resistorBands.Colors.Contains(valueFromBands.Multiplier)
    || !resistorBands.Colors.Contains(valueFromBands.Tolerance)
    || toleranceIx == 0 || toleranceIx == 3 || toleranceIx == 4 || toleranceIx == 5 || toleranceIx == 9)
    {
        return Results.BadRequest();
    }

    var firstBandValue = resistorBands.BandValues[Array.IndexOf(resistorBands.Colors, valueFromBands.FirstBand)];
    var secondBandValue = resistorBands.BandValues[Array.IndexOf(resistorBands.Colors, valueFromBands.SecondBand)];   
    var thirdBandValue = -1;
    if (valueFromBands.ThirdBand != null)
    {
        var thirdBandIx = Array.IndexOf(resistorBands.Colors, valueFromBands.ThirdBand);
        if (resistorBands.Colors.Contains(valueFromBands.ThirdBand)
        && thirdBandIx != 10 && thirdBandIx != 11)
        {
            thirdBandValue = resistorBands.BandValues[Array.IndexOf(resistorBands.Colors, valueFromBands.ThirdBand)];
        }
        else {
            return Results.BadRequest();
        }
    }
    var multiplier = resistorBands.Multipliers[Array.IndexOf(resistorBands.Colors, valueFromBands.Multiplier)];
    var tolerance = resistorBands.Tolerances[Array.IndexOf(resistorBands.Colors, valueFromBands.Tolerance)];

    var resistorValue = 0.0;
    if (thirdBandValue == -1)
    {
        resistorValue = (firstBandValue * 10 + secondBandValue) * multiplier;
    }
    else 
    {
        resistorValue = (firstBandValue * 100 + secondBandValue * 10 + thirdBandValue) * multiplier;
    }
    
    return Results.Ok(new ValueFromBandsRes(resistorValue, tolerance));
})
    .WithName("CalculateFromBands")
    .WithTags("Resistors")
    .Produces<ValueFromBandsRes>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithOpenApi(o => 
    {
        o.Summary = "Calculates the resistor value based on given color bands (using POST).";
        o.RequestBody.Required = true;

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor value could be decoded correctly";
        o.Responses[((int)StatusCodes.Status400BadRequest).ToString()].Description = "The request body contains invalid data";

        return o;
    });

app.MapGet("/resistors/value-from-brands", (string firstBand, string secondBand, string? thirdBand, string multiplier, string tolerance, ResistorBands resistorBands) =>
{ 
    if (firstBand == null || secondBand == null || multiplier == null || tolerance == null)
    {
        return Results.BadRequest();
    }

    var firstBandIx = Array.IndexOf(resistorBands.Colors, firstBand);
    var secondBandIx = Array.IndexOf(resistorBands.Colors, secondBand);
    var toleranceIx = Array.IndexOf(resistorBands.Colors, tolerance);

    if (!resistorBands.Colors.Contains(firstBand)
    || firstBandIx == 10 || firstBandIx == 11
    || !resistorBands.Colors.Contains(secondBand)
    || secondBandIx == 10 || secondBandIx == 11
    || !resistorBands.Colors.Contains(multiplier)
    || !resistorBands.Colors.Contains(tolerance)
    || toleranceIx == 0 || toleranceIx == 3 || toleranceIx == 4 || toleranceIx == 5 || toleranceIx == 9)
    {
        return Results.BadRequest();
    }
    
    var firstBandValue = resistorBands.BandValues[Array.IndexOf(resistorBands.Colors, firstBand)];
    var secondBandValue = resistorBands.BandValues[Array.IndexOf(resistorBands.Colors, secondBand)];   
    var thirdBandValue = -1;
    if (thirdBand != null)
    {
        var thirdBandIx = Array.IndexOf(resistorBands.Colors, thirdBand);
        if (resistorBands.Colors.Contains(thirdBand)
        && thirdBandIx != 10 && thirdBandIx != 11)
        {
            thirdBandValue = resistorBands.BandValues[Array.IndexOf(resistorBands.Colors, thirdBand)];
        }
        else {
            return Results.BadRequest();
        }
    }
    var multiplierValue = resistorBands.Multipliers[Array.IndexOf(resistorBands.Colors, multiplier)];
    var toleranceValue = resistorBands.Tolerances[Array.IndexOf(resistorBands.Colors, tolerance)];

    var resistorValue = 0.0;
    if (thirdBandValue == -1)
    {
        resistorValue = (firstBandValue * 10 + secondBandValue) * multiplierValue;
    }
    else 
    {
        resistorValue = (firstBandValue * 100 + secondBandValue * 10 + thirdBandValue) * multiplierValue;
    }
    
    return Results.Ok(new ValueFromBandsRes(resistorValue, toleranceValue));
})
    .WithName("GetCalculateFromBrands")
    .WithTags("Resistors")
    .Produces<ValueFromBandsRes>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithOpenApi(o => 
    {
        o.Summary = "Calculates the resistor value based on given color bands (using GET).";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor value could be decoded correctly";
        o.Responses[((int)StatusCodes.Status400BadRequest).ToString()].Description = "The request body contains invalid data";

        o.Parameters[0].Description = "Color of the 1st band";
        o.Parameters[1].Description = "Color of the 2nd band";
        o.Parameters[2].Description = "Color of the 3rd band. Note that this band can be left out for 4-band-coded resistors.";
        o.Parameters[3].Description = "Color of the multiplier band";
        o.Parameters[4].Description = "Color of the tolerance band";

        return o;
    });

app.MapPost("resistors/bands-from-value", (BandsFromValueReq bandsFromValue, ResistorBands ResistorBands) => 
{
    var resistorVal = bandsFromValue.ResistorValue;
    var multiplier = 1.0;

    var intPart = Math.Truncate(resistorVal);
    var decPart = resistorVal - intPart;

    var resValString = bandsFromValue.ResistorValue.ToString();
    if (decPart > 0)
    {
        var afterDecLength = resValString.Substring(resValString.IndexOf(".")).Length - 1;
        if (afterDecLength > 2)
        {
            return Results.BadRequest();
        }
        if (afterDecLength == 2 && bandsFromValue.NumberOfBands != 5)
        {
            return Results.BadRequest();
        }

        var c = 0;
        while (decPart > 0 && c < bandsFromValue.NumberOfBands - 3)
        {
            resistorVal *= 10;
            decPart *= 10;
            decPart = decPart - Math.Truncate(decPart);
            multiplier /= 10;
            c++;
        }
    }
    else 
    {
        var c = 0;
        while (resistorVal % 10 == 0 && c <= bandsFromValue.NumberOfBands - 3)
        {
            resistorVal /= 10;
            multiplier *= 10;
            c++;
        }
        if (resValString.Length == 2 && bandsFromValue.NumberOfBands != 4 || resValString.Length == 3 && bandsFromValue.NumberOfBands != 5)
        {
            return Results.BadRequest();
        }
    }
    
    if (bandsFromValue.NumberOfBands == 5)
    {
        return Results.Ok(new BandsFromValueRes(ResistorBands.Colors[(int)resistorVal / 100], ResistorBands.Colors[(int)resistorVal % 100 / 10], ResistorBands.Colors[(int)resistorVal % 10], ResistorBands.Colors[Array.IndexOf(ResistorBands.Multipliers, multiplier)], ResistorBands.Colors[Array.IndexOf(ResistorBands.Tolerances, bandsFromValue.Tolerance)]));
    }
    else if (bandsFromValue.NumberOfBands == 4)
    {
        return Results.Ok(new BandsFromValueRes(ResistorBands.Colors[(int)resistorVal / 10], ResistorBands.Colors[(int)resistorVal % 10], null, ResistorBands.Colors[Array.IndexOf(ResistorBands.Multipliers, multiplier)], ResistorBands.Colors[Array.IndexOf(ResistorBands.Tolerances, bandsFromValue.Tolerance)]));
    }
    else 
    {
        return Results.BadRequest();
    }
})
    .WithName("CalculateBandsFromValue")
    .WithTags("Resistors")
    .Produces<BandsFromValueRes>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithOpenApi(o => 
    {
        o.Summary = "Calculates the bands for a resistor based on its value";

        o.Responses[((int)StatusCodes.Status200OK).ToString()].Description = "Resistor bands could be calculated";
        o.Responses[((int)StatusCodes.Status400BadRequest).ToString()].Description = "The request body contains invalid data";

        return o;
    });

app.Run();

class ResistorBands
{
    /// <summary>
    /// All colors for bands on resistors
    /// </summary>
    public string[] Colors { get; set; } = new string[] { "black", "brown", "red", "orange", "yellow", "green", "blue", "violet", "gray", "white", "gold", "silver" };
    /// <summary>
    /// Band values for each color
    /// </summary>
    public int[] BandValues { get; set; } = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0 };
    /// <summary>
    /// Multipliers for each color
    /// </summary>
    public double[] Multipliers { get; set; } = new double[] { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100_000_000, 1_000_000_000, 0.1, 0.01 };
    /// <summary>
    /// Tolerances for each color
    /// </summary>
    public double[] Tolerances { get; set; } = new double[] { 0, 1, 2, 0, 0, 0.5, 0.25, 0.1, 0.05, 0, 5, 10 };
}

record ColorDetails(int BandValue, double Multiplier, double Tolerance);
record ValueFromBandsReq(string FirstBand, string SecondBand, string ThirdBand, string Multiplier, string Tolerance);
record ValueFromBandsRes(double ResistorValue, double Tolerance);
record BandsFromValueReq(double ResistorValue, double Tolerance, int NumberOfBands);
record BandsFromValueRes(string FirstBand, string SecondBand, string? ThirdBand, string Multiplier, string Tolerance);
