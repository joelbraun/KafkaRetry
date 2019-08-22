using System;

public interface IEmployeeSqlRepository 
{
    bool UpdateEmployeeName(int id, string employeeName, DateTime timestamp);
}