﻿using System;
using System.Linq;
using System.Net.Http;
using cafe.Client;
using cafe.Shared;
using NLog;

namespace cafe.CommandLine
{
    public abstract class Option
    {
        private static readonly Logger Logger = LogManager.GetLogger(typeof(SchedulerWaiter).FullName);

        private readonly string _helpText;
        private readonly OptionSpecification _specification;

        protected Option(OptionSpecification specification, string helpText)
        {
            _specification = specification;
            _helpText = helpText;
        }

        public Result Run(params string[] args)
        {
            if (_specification.HelpRequested(args))
            {
                ShowHelp();
                return Result.Successful();
            }
            Result result = null;
            var description = ToDescription(args);
            try
            {
                Presenter.NewLine();
                Presenter.ShowMessage($"{description}:", Logger);
                Presenter.NewLine();
                result = RunCore(args);
            }
            catch (AggregateException ae)
            {
                var inner = ae.InnerExceptions.FirstOrDefault(e => e is HttpRequestException);
                if (inner != null)
                {
                    result = BadConnectionFailureFromException(ae);
                }
                else
                {
                    result = GenericFailureFromException(ae);
                }
            }
            catch (HttpRequestException re)
            {
                result = BadConnectionFailureFromException(re);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An unexpected error occurred while executing this option");
                result = GenericFailureFromException(ex);
            }
            finally
            {
                Presenter.NewLine();
                Presenter.ShowMessage($"Finished {description} with result: {result}", Logger);
            }
            return result;
        }

        private Result BadConnectionFailureFromException(Exception ex)
        {
            Logger.Info(ex, "Could not connect to the server and thus got exception");

            return Result.Failure("A connection to the server could not be made. Make sure it's running.");
        }

        private static Result GenericFailureFromException(Exception ex)
        {
            return Result.Failure($"An unexpected error occurred while executing this option: {ex.Message}");
        }

        protected abstract string ToDescription(string[] args);

        protected abstract Result RunCore(string[] args);

        public virtual void ShowHelp()
        {
            Console.Out.WriteLine($"Help: {_helpText}");
        }

        public bool IsSatisfiedBy(string[] args)
        {
            return _specification.IsSatisfiedBy(args);
        }

        public override string ToString()
        {
            return $"{_specification} ({_helpText})";
        }
    }
}