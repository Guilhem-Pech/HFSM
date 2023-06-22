namespace HFSM;

public class Optionnal<T>
{
    private bool m_isSet = false;
    private T? m_value;

    public bool HasValue()
    {
        return m_isSet;
    }

    public T SetValue(T _value)
    {
        m_value = _value;
        m_isSet = true;
        return m_value;
    }

    public void ClearValue()
    {
        m_value = default;
        m_isSet = false;
    }
        
    public Optionnal(T _value)
    {
        m_isSet = true;
        m_value = _value;
    }
    
    public Optionnal(){}

    public T Value()
    {
        return m_value ?? throw new InvalidOperationException();
    }
}