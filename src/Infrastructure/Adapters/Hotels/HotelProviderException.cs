namespace WhereToStayInJapan.Infrastructure.Adapters.Hotels;

public class HotelProviderException(string message, Exception? inner = null)
    : Exception(message, inner);
