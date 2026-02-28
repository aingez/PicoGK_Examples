public class FixtureObject
{

}

public class BasePlate
{
    // nothing here yet
}

public class FixtureMaker
{
    public FixtureMaker( // constructure
        BasePlate oPlate,
        FixtureObject oObject
    )
    {
        m_oPlate = oPlate;
        m_oObject = oObject;
    }

    public void Run()
    {
        BasePlate oBase = new();
        FixtureObject oObject = new();

        FixtureMaker oMaker = new(oBase, oObject);
        oMaker.Run();
    }

    BasePlate m_oPlate;
    FixtureObject m_oObject;
}